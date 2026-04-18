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
public class BoxColliderApplier : IComponentApplier
{
    public Type TargetType => typeof(BoxCollider);
    private Dictionary<string, Action<BoxCollider, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<BoxCollider, FieldSnapshot>>()
    {
        { "m_Center", (c, field) => c.center = field.GetAs<Vector3>() },
        { "m_Size", (c, field) => c.size = field.GetAs<Vector3>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => false;
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((BoxCollider)target, field);
        return true;
    }
}