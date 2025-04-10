// RobotModel.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

public class RobotModel
{
    public string Name { get; set; }
    public Dictionary<string, Link> Links { get; set; }
    public Dictionary<string, Joint> Joints { get; set; }
}

public class Link
{
    public Visual Visual { get; set; }
}

public class Visual
{
    public Geometry Geometry { get; set; }
    public Origin Origin { get; set; }
}

public class Geometry
{
    public string Shape { get; set; }

    public string Size { get; set; }

}

public class Joint
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Parent { get; set; }
    public string Child { get; set; }
    public float[] Axis { get; set; }
    public Origin Origin { get; set; }
}

public class Origin
{
    public float[] Xyz { get; set; }

    public float[] Rpy { get; set; }
}



public static class URDFLoader
{
    public static RobotModel LoadFromJson(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var model = JsonSerializer.Deserialize<RobotModel>(json, options);

        foreach (var kvp in model.Joints)
            kvp.Value.Name = kvp.Key;

        return model;
    }
}

public static class FKEngine
{
    public static Transform BuildTransform(Origin origin, float jointAngle = 0, float[] axis = null)
    {
        if (origin == null)
            origin = new Origin { Xyz = new float[] { 0, 0, 0 }, Rpy = new float[] { 0, 0, 0 } };

        var rpy = origin.Rpy ?? new float[] { 0, 0, 0 };
        var xyz = origin.Xyz ?? new float[] { 0, 0, 0 };

        Transform r = Transform.Rotation(rpy[2], Vector3d.ZAxis, Point3d.Origin) *
                      Transform.Rotation(rpy[1], Vector3d.YAxis, Point3d.Origin) *
                      Transform.Rotation(rpy[0], Vector3d.XAxis, Point3d.Origin);

        Transform t = Transform.Translation(new Vector3d(xyz[0], xyz[1], xyz[2]));

        Transform jointRot = Transform.Identity;
        if (axis != null && jointAngle != 0)
        {
            Vector3d axisVec = new Vector3d(axis[0], axis[1], axis[2]);
            jointRot = Transform.Rotation(jointAngle, axisVec, Point3d.Origin);
        }

        return t * r * jointRot;
    }

    public static Dictionary<string, Transform> ComputeFK(RobotModel model, Dictionary<string, float> jointValues, string baseLink)
    {
        var transforms = new Dictionary<string, Transform>();
        transforms[baseLink] = Transform.Identity;

        void Traverse(string parent)
        {
            if (!transforms.ContainsKey(parent))
            {
                RhinoApp.WriteLine($"FK skipped: missing parent transform for '{parent}'");
                return;
            }

            foreach (var joint in model.Joints.Values.Where(j => j.Parent == parent))
            {
                string child = joint.Child;
                float angle = (joint.Type == "revolute" || joint.Type == "continuous") &&
                              jointValues.ContainsKey(joint.Name)
                              ? jointValues[joint.Name] : 0;

                var jointTransform = BuildTransform(joint.Origin, angle, joint.Axis);
                transforms[child] = transforms[parent] * jointTransform;

                Traverse(child);
            }
        }

        Traverse(baseLink);
        return transforms;
    }

    public static void DrawRobot(RobotModel model, Dictionary<string, Transform> transforms)
    {
        var doc = RhinoDoc.ActiveDoc;
        doc.Objects.Clear();

        foreach (var kvp in transforms)
        {
            var linkName = kvp.Key;
            var t = kvp.Value;
            var origin = t * Point3d.Origin;

            //Draw only FK debug markers
            doc.Objects.AddSphere(new Sphere(origin, 0.02));
        }

        doc.Views.Redraw();
    }

    public static string ResolveMeshPath(string urdfPath)
    {
        if (string.IsNullOrWhiteSpace(urdfPath))
            return null;

        string relativePath = urdfPath.Replace("package://ur_description/", "");
        string baseDir = @"C:\Users\Chris\source\repos\VOLTtools\VOLTtools\Resources\ur_description\"; 
        return Path.Combine(baseDir, relativePath.Replace("/", "\\"));
    }
}

