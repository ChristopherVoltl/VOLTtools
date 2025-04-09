// URVisualizer.cs
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;

public class URVisualizer
{
    public static void DrawRobot(Transform[] transforms)
    {
        var doc = RhinoDoc.ActiveDoc;
        doc.Objects.Clear();

        Point3d prev = Point3d.Origin;
        foreach (var t in transforms)
        {
            Point3d pt = new Point3d(0, 0, 0);
            pt = t * pt;
            doc.Objects.AddLine(prev, pt);
            doc.Objects.AddSphere(new Sphere(pt, 0.02));
            prev = pt;
        }

        doc.Views.Redraw();
    }
}
