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
    private HashSet<string> supportedFields { get; } = new HashSet<string>()
    {
        "m_LocalPosition",
        "m_LocalRotation",
        "m_LocalScale",
    };

    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        "m_ConstrainProportionsScale",
    };

    public bool Supports(string path) => supportedFields.Contains(path) || ignoredFields.Contains(path);

    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;

        Transform t = (Transform)target;

        switch (field.path)
        {
            case "m_LocalPosition":
                t.localPosition = field.GetAs<Vector3>();
                return true;
            case "m_LocalRotation":
                t.localRotation = field.GetAs<Quaternion>();
                return true;
            case "m_LocalScale":
                t.localScale = field.GetAs<Vector3>();
                return true;
        }

        return false;
    }
}