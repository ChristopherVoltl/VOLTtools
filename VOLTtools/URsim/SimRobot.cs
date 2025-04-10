using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Types.Transforms;
using Rhino;
using Rhino.Geometry;

public class SimRobot
{
    private readonly RobotModel _model;
    private readonly Dictionary<string, Guid> _linkMeshIds = new Dictionary<string, Guid>();
    private readonly Dictionary<string, Transform> _visualOffsets = new Dictionary<string, Transform>();

    private readonly Dictionary<string, Mesh> _originalMeshes = new Dictionary<string, Mesh>();



    public SimRobot(RobotModel model)
    {
        _model = model;

        // Initialize mesh instances
        foreach (var link in _model.Links)
        {
            string linkName = link.Key;
            var visual = link.Value.Visual;
            var sizePath = visual?.Geometry?.Size;

            if (sizePath == null || sizePath.Length == 0) continue;

            // Join parts into mesh path string
            string pathStr = string.Join(" ", sizePath);
            string path = FKEngine.ResolveMeshPath(pathStr);

            var original = MeshCache.Get(path);
            if (original == null) continue;

            _originalMeshes[linkName] = original;

            Guid id = RhinoDoc.ActiveDoc.Objects.AddMesh(original.DuplicateMesh());
            _linkMeshIds[linkName] = id;
            _visualOffsets[linkName] = FKEngine.BuildTransform(visual.Origin);
        }
    }

    public void Update(Dictionary<string, Transform> fkTransforms)
    {
        foreach (var kvp in _linkMeshIds)
        {
            string linkName = kvp.Key;
            Guid objId = kvp.Value;

            if (!fkTransforms.TryGetValue(linkName, out var fk)) continue;
            if (!_visualOffsets.TryGetValue(linkName, out var visual)) continue;
            if (!_originalMeshes.TryGetValue(linkName, out var original)) continue;

            // Delete existing mesh
            RhinoDoc.ActiveDoc.Objects.Delete(objId, true);

            // Transform from clean mesh
            var fresh = original.DuplicateMesh();
            fresh.Transform(fk * visual);

            // Add and track new ID
            var newId = RhinoDoc.ActiveDoc.Objects.AddMesh(fresh);
            _linkMeshIds[linkName] = newId;
        }

        RhinoDoc.ActiveDoc.Views.Redraw();
    }
}