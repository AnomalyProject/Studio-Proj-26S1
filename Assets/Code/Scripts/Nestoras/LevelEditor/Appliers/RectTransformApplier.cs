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
public class RectTransformApplier : IComponentApplier
{
    public Type TargetType => typeof(RectTransform);
    private Dictionary<string, Action<RectTransform, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<RectTransform, FieldSnapshot>>()
    {
        { "m_AnchorMin", (rt, field) => rt.anchorMin = field.GetAs<Vector2>() },
        { "m_AnchorMax", (rt, field) => rt.anchorMax = field.GetAs<Vector2>() },
        { "m_AnchoredPosition", (rt, field) => rt.anchoredPosition = field.GetAs<Vector2>() },
        { "m_SizeDelta", (rt, field) => rt.sizeDelta = field.GetAs<Vector2>() },
        { "m_Pivot", (rt, field) => rt.pivot = field.GetAs<Vector2>() },
    };
    public bool Supports(string path) => supportedFields.ContainsKey(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((RectTransform)target, field);
        return true;
    }
}