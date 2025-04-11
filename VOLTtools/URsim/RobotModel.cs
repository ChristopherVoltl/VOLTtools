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
using static FKEngine;

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

    [JsonConverter(typeof(FloatArrayFromStringConverter))]
    public float[] Axis { get; set; }

    public Origin Origin { get; set; }
}

public class Origin
{
    [JsonConverter(typeof(FloatArrayFromStringConverter))]
    public float[] Xyz { get; set; }

    [JsonConverter(typeof(FloatArrayFromStringConverter))]
    public float[] Rpy { get; set; }
}



public static class URDFLoader
{
    public static RobotModel LoadFromJson(string path)
    {
        RhinoApp.WriteLine($"📂 Attempting to load JSON from: {path}");

        if (!File.Exists(path))
        {
            RhinoApp.WriteLine("❌ File does not exist.");
            return null;
        }

        var json = File.ReadAllText(path);

        // 🔍 Inspect the root structure
        var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            RhinoApp.WriteLine($"[ROOT KEY] {prop.Name}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var model = JsonSerializer.Deserialize<RobotModel>(json, options);

        if (model == null)
        {
            RhinoApp.WriteLine("❌ Deserialization returned null.");
            return null;
        }

        if (model.Joints == null)
        {
            RhinoApp.WriteLine("❌ RobotModel.Joints is null.");
        }
        else
        {
            RhinoApp.WriteLine($"✅ Loaded {model.Joints.Count} joints.");
        }

        foreach (var kvp in model.Joints)
        {
            kvp.Value.Name = kvp.Key;

        }
        foreach (var joint in model.Joints.Values)
        {
            if (joint.Axis == null || joint.Axis.Length != 3)
            {
                RhinoApp.WriteLine($"❌ Joint {joint.Name} has invalid axis.");
                joint.Axis = new float[] { 0, 0, 1 }; // or proper value
            }
        }

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

        // Apply URDF-style extrinsic rotations: Z → Y → X
        Transform r = Transform.Rotation(rpy[2], Vector3d.ZAxis, Point3d.Origin) *
                      Transform.Rotation(rpy[1], Vector3d.YAxis, Point3d.Origin) *
                      Transform.Rotation(rpy[0], Vector3d.XAxis, Point3d.Origin);

        Transform t = Transform.Translation(new Vector3d(xyz[0], xyz[1], xyz[2]));

        Transform jointRot = Transform.Identity;

        // 🔧 Axis fallback if missing or invalid
        if (axis == null || axis.Length != 3 || axis.All(v => Math.Abs(v) < 1e-6))
        {
            //RhinoApp.WriteLine("WARNING: Joint axis missing or invalid, defaulting to [0,0,1]");
            axis = new float[] { 0, 0, 1 };
        }

        if (jointAngle != 0)
        {
            Vector3d axisVec = new Vector3d(axis[0], axis[1], axis[2]);
            jointRot = Transform.Rotation(jointAngle, axisVec, Point3d.Origin);

            //RhinoApp.WriteLine($"Axis: {axisVec.X}, {axisVec.Y}, {axisVec.Z} | θ = {jointAngle:0.0000}");
        }

        return t * r * jointRot;
    }

    public static Dictionary<string, Transform> ComputeFK(RobotModel model, Dictionary<string, float> jointValues, string baseLink)
    {
        if (model?.Joints == null)
        {
            RhinoApp.WriteLine("❌ ComputeFK: model.Joints is null");
            return new Dictionary<string, Transform>();
        }

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

                var pos = transforms[child] * Point3d.Origin;
                //RhinoApp.WriteLine($"FK[{child}]: {pos}");
            }
        }

        Traverse(baseLink);
        return transforms;

    }

    public static List<Joint> GetOrderedJoints(RobotModel model, string baseLink)
    {
        var ordered = new List<Joint>();

        void Traverse(string parent)
        {
            foreach (var joint in model.Joints.Values.Where(j => j.Parent == parent))
            {
                if (joint.Type == "revolute" || joint.Type == "continuous")
                    ordered.Add(joint);

                Traverse(joint.Child);
            }
        }

        Traverse(baseLink);
        return ordered;
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

    public class FloatArrayFromStringConverter : JsonConverter<float[]>
    {
        public override float[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string str = reader.GetString();
                return str.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => float.TryParse(s, out var f) ? f : 0f)
                          .ToArray();
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var values = new List<float>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.Number)
                        values.Add(reader.GetSingle());
                }
                return values.ToArray();
            }

            return Array.Empty<float>();
        }

        public override void Write(Utf8JsonWriter writer, float[] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var f in value)
                writer.WriteNumberValue(f);
            writer.WriteEndArray();
        }
    }
}

