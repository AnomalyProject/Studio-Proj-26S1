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
public class CapsuleColliderApplier : IComponentApplier
{
    public Type TargetType => typeof(CapsuleCollider);
    private Dictionary<string, Action<CapsuleCollider, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<CapsuleCollider, FieldSnapshot>>()
    {
        { "m_Center", (c, field) => c.center = field.GetAs<Vector3>() },
        { "m_Radius", (c, field) => c.radius = field.GetAs<float>() },
        { "m_Height", (c, field) => c.height = field.GetAs<float>() },
        { "m_Direction", (c, field) => c.direction = field.GetAs<int>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((CapsuleCollider)target, field);
        return true;
    }
}