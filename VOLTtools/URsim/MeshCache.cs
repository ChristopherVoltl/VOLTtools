using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;

public static class MeshCache
{
    private static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();

    public static Mesh Get(string path)
    {
        if (_cache.TryGetValue(path, out var mesh))
            return mesh;

        mesh = STLImporter.LoadSTL(path);
        if (mesh != null)
        {
            _cache[path] = mesh;
        }

        if (_cache.ContainsKey(path))
            RhinoApp.WriteLine($"Mesh from cache: {path}");
        else
            RhinoApp.WriteLine($"First time loading mesh: {path}");

        return mesh;
    }



    public static void Clear()
    {
        _cache.Clear();
    }
}