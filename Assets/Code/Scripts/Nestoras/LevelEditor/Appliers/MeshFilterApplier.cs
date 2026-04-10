using System.Collections.Generic;
using System;
using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine;

[Preserve] // Avoid stripping type from build
public class MeshFilterApplier : IComponentApplier
{
    public Type TargetType => typeof(MeshFilter);
    public HashSet<string> supportedFields { get; } = new HashSet<string>()
    {
        "m_Mesh",
        "m_SharedMesh",
    };

    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        
    };

    public bool Supports(string path) => supportedFields.Contains(path) || ignoredFields.Contains(path);

    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;

        MeshFilter f = (MeshFilter)target;

        switch (field.path)
        {
            case "m_Mesh":
            case "m_SharedMesh":
                f.sharedMesh = ResolveMesh(field);
                return true;
        }

        return false;
    }

    #region Helpers
    private Mesh ResolveMesh(FieldSnapshot field)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.EntityIdToObject(field.objectReferenceInstanceID) as Mesh;
#else
        return null; // runtime needs asset registry / GUID system
#endif
    }
    #endregion
}