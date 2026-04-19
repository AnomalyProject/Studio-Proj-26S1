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
public class SpringJointApplier : IComponentApplier
{
    public Type TargetType => typeof(SpringJoint);
    private Dictionary<string, Action<SpringJoint, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<SpringJoint, FieldSnapshot>>()
    {
        { "m_Spring", (sj, field) => sj.spring = field.GetAs<float>() },
        { "m_Damper", (sj, field) => sj.damper = field.GetAs<float>() },
        { "m_MinDistance", (sj, field) => sj.minDistance = field.GetAs<float>() },
        { "m_MaxDistance", (sj, field) => sj.maxDistance = field.GetAs<float>() },
        { "m_Tolerance", (sj, field) => sj.tolerance = field.GetAs<float>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Ignores(string path) => false;
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((SpringJoint)target, field);
        return true;
    }
}