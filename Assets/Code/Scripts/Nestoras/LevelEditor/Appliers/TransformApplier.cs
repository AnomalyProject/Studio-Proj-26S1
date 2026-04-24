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
public class TransformApplier : IComponentApplier
{
    public Type TargetType => typeof(Transform);
    private Dictionary<string, Action<Transform, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Transform, FieldSnapshot>>()
    {
        { "m_LocalPosition", (t, field) => t.localPosition = field.GetAs<Vector3>() },
        { "m_LocalRotation", (t, field) => t.localRotation = field.GetAs<Quaternion>() },
        { "m_LocalScale", (t, field) => t.localScale = field.GetAs<Vector3>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => path == "m_ConstrainProportionsScale";
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Transform)target, field);
        return true;
    }
}