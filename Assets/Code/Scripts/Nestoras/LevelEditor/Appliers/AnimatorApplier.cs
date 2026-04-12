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
public class AnimatorApplier : IComponentApplier
{
    public Type TargetType => typeof(Animator);
    private Dictionary<string, Action<Animator, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Animator, FieldSnapshot>>()
    {
        { "m_Avatar", (a, field) => a.avatar = field.GetAsObject() as Avatar },
        { "m_Controller", (a, field) => a.runtimeAnimatorController = field.GetAsObject() as RuntimeAnimatorController },
        { "m_CullingMode", (a, field) => a.cullingMode = (AnimatorCullingMode)field.GetAs<int>() },
        { "m_UpdateMode", (a, field) => a.updateMode = (AnimatorUpdateMode)field.GetAs<int>() },
        { "m_ApplyRootMotion", (a, field) => a.applyRootMotion = field.GetAs<bool>() },
        { "m_StabilizeFeet", (a, field) => a.stabilizeFeet = field.GetAs<bool>() },
        { "m_AnimatePhysics", (a, field) => a.animatePhysics = field.GetAs<bool>() },
        { "m_KeepAnimatorStateOnDisable", (a, field) => a.keepAnimatorStateOnDisable = field.GetAs<bool>() },
        { "m_WriteDefaultValuesOnDisable", (a, field) => a.writeDefaultValuesOnDisable = field.GetAs<bool>() },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {
        "m_LinearVelocityBlending",
        "m_WarningMessage",
        "m_HasTransformHierarchy",
        "m_AllowConstantClipSamplingOptimization",
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path) || ignoredFields.Contains(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;
        Animator a = (Animator)target;
        supportedFields[field.path](a, field);
        return false;
    }
}