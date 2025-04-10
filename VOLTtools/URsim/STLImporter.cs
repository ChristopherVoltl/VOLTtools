using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Rhino.Geometry;

public static class STLImporter
{
    public static Mesh LoadSTL(string path)
    {
        if (!File.Exists(path))
            return null;

        using (var stream = File.OpenRead(path))
        {
            if (IsAsciiSTL(stream))
            {
                stream.Position = 0;
                return LoadAsciiSTL(stream);
            }
            else
            {
                stream.Position = 0;
                return LoadBinarySTL(stream);
            }
        }
    }

    private static bool IsAsciiSTL(Stream stream)
    {
        using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
        {
            char[] buffer = new char[5];
            reader.ReadBlock(buffer, 0, 5);
            return new string(buffer).ToLower().Contains("solid");
        }
    }

    private static Mesh LoadAsciiSTL(Stream stream)
    {
        var mesh = new Mesh();
        using (var reader = new StreamReader(stream))
        {
            Point3d[] points = new Point3d[3];
            int vertexIndex = 0;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine().Trim();
                if (line.StartsWith("vertex"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4)
                    {
                        double x = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        double y = double.Parse(parts[2], CultureInfo.InvariantCulture);
                        double z = double.Parse(parts[3], CultureInfo.InvariantCulture);
                        points[vertexIndex++] = new Point3d(x, y, z);
                    }

                    if (vertexIndex == 3)
                    {
                        mesh.Vertices.AddVertices(points);
                        mesh.Faces.AddFace(mesh.Vertices.Count - 3, mesh.Vertices.Count - 2, mesh.Vertices.Count - 1);
                        vertexIndex = 0;
                    }
                }
            }
        }

        mesh.Normals.ComputeNormals();
        mesh.Compact();
        return mesh.IsValid ? mesh : null;
    }

    private static Mesh LoadBinarySTL(Stream stream)
    {
        var mesh = new Mesh();
        using (var reader = new BinaryReader(stream))
        {
            reader.ReadBytes(80); // header
            uint triangleCount = reader.ReadUInt32();

            for (int i = 0; i < triangleCount; i++)
            {
                // skip normal
                reader.ReadSingle(); reader.ReadSingle(); reader.ReadSingle();

                var p1 = ReadPoint(reader);
                var p2 = ReadPoint(reader);
                var p3 = ReadPoint(reader);

                int i0 = mesh.Vertices.Add(p1);
                int i1 = mesh.Vertices.Add(p2);
                int i2 = mesh.Vertices.Add(p3);

                mesh.Faces.AddFace(i0, i1, i2);

                reader.ReadUInt16(); // attribute byte count
            }
        }

        mesh.Normals.ComputeNormals();
        mesh.Compact();
        return mesh.IsValid ? mesh : null;
    }

    private static Point3d ReadPoint(BinaryReader reader)
    {
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        return new Point3d(x, y, z);
    }
}
