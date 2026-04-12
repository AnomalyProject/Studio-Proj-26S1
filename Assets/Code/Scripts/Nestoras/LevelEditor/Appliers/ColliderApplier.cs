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
public class ColliderApplier : IComponentApplier
{
    public Type TargetType => typeof(Collider);
    private Dictionary<string, Action<Collider, FieldSnapshot>> supportedFields { get; } = new Dictionary<string, Action<Collider, FieldSnapshot>>()
    {
        { "m_IsTrigger", (c, field) => c.isTrigger = field.GetAs<bool>() },
        { "m_Material", (c, field) => c.sharedMaterial = field.GetAsObject() as PhysicsMaterial },
        { "m_ContactOffset", (c, field) => c.contactOffset = field.GetAs<float>() },
        { "m_LayerOverridePriority", (c, field) => c.layerOverridePriority = field.GetAs<int>() },
        { "m_IncludeLayers", (c, field) => c.includeLayers = field.GetAs<int>() },
        { "m_ExcludeLayers", (c, field) => c.excludeLayers = field.GetAs<int>() },
        { "m_ProvidesContacts", (c, field) => c.providesContacts = field.GetAs<bool>() },
    };
    private HashSet<string> ignoredFields { get; } = new HashSet<string>() { "m_Enabled" };
    public bool Supports(string path) => supportedFields.ContainsKey(path) || ignoredFields.Contains(path);
    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;
        if (!supportedFields.ContainsKey(field.path)) return false;
        supportedFields[field.path]((Collider)target, field);
        return true;
    }
}