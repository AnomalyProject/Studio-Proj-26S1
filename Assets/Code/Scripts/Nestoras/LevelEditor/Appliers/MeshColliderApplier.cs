using System.Collections.Generic;
using System;
using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Applier script to translate set commands by <see cref="ModificationApplier"/> (SerializedProperty path and value) into actual API calls that work in standalone builds.
/// </summary>
[Preserve] // Avoid stripping type from build
public class MeshColliderApplier : IComponentApplier
{
    public Type TargetType => typeof(MeshCollider);
    private Dictionary<string, Action<MeshCollider, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<MeshCollider, FieldSnapshot>>()
    {
        { "m_Mesh", (c, field) => c.sharedMesh = field.GetAsObject() as Mesh },
        { "m_Convex", (c, field) => c.convex = field.GetAs<bool>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((MeshCollider)target, field);
        return true;
    }
}