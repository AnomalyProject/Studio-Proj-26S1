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
public class SphereColliderApplier : IComponentApplier
{
    public Type TargetType => typeof(SphereCollider);
    private Dictionary<string, Action<SphereCollider, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<SphereCollider, FieldSnapshot>>()
    {
        { "m_Center", (c, field) => c.center = field.GetAs<Vector3>() },
        { "m_Radius", (c, field) => c.radius = field.GetAs<float>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((SphereCollider)target, field);
        return true;
    }
}