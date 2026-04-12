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
public class CharacterControllerApplier : IComponentApplier
{
    public Type TargetType => typeof(CharacterController);
    private Dictionary<string, Action<CharacterController, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<CharacterController, FieldSnapshot>>()
    {
        { "m_Height", (c, field) => c.height = field.GetAs<float>() },
        { "m_Radius", (c, field) => c.radius = field.GetAs<float>() },
        { "m_SlopeLimit", (c, field) => c.slopeLimit = field.GetAs<float>() },
        { "m_StepOffset", (c, field) => c.stepOffset = field.GetAs<float>() },
        { "m_SkinWidth", (c, field) => c.skinWidth = field.GetAs<float>() },
        { "m_MinMoveDistance", (c, field) => c.minMoveDistance = field.GetAs<float>() },
        { "m_Center", (c, field) => c.center = field.GetAs<Vector3>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((CharacterController)target, field);
        return true;
    }
}