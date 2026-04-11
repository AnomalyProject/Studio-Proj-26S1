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
    public HashSet<string> supportedFields { get; } = new HashSet<string>()
    {
        "m_Center",
        "m_Size",
    };

    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        
    };

    public bool Supports(string path) => supportedFields.Contains(path) || ignoredFields.Contains(path);

    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;

        BoxCollider c = (BoxCollider)target;

        switch (field.path)
        {
            case "m_Center":
                c.center = field.GetAs<Vector3>();
                return true;

            case "m_Size":
                c.size = field.GetAs<Vector3>();
                return true;
        }

        return false;
    }
}