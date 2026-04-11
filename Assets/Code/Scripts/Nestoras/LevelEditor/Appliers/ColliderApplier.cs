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
    public HashSet<string> supportedFields { get; } = new HashSet<string>()
    {
        "m_IsTrigger",
        "m_Material",
        "m_ContactOffset",
        "m_LayerOverridePriority",
        "m_IncludeLayers",
        "m_ExcludeLayers",
        "m_ProvidesContacts",
    };

    private HashSet<string> ignoredFields { get; } = new HashSet<string>()
    {

    };

    public bool Supports(string path) => supportedFields.Contains(path) || ignoredFields.Contains(path);

    public bool Apply(Component target, FieldSnapshot field)
    {
        if (ignoredFields.Contains(field.path)) return true;

        Collider c = (Collider)target;

        switch (field.path)
        {
            case "m_IsTrigger":
                c.isTrigger = field.GetAs<bool>();
                return true;

            case "m_Material":
                c.sharedMaterial = field.GetAsObject() as PhysicsMaterial;
                return true;

            case "m_ContactOffset":
                c.contactOffset = field.GetAs<float>();
                return true;

            case "m_LayerOverridePriority":
                c.layerOverridePriority = field.GetAs<int>();
                return true;

            case "m_IncludeLayers":
                c.includeLayers = field.GetAs<int>();
                return true;

            case "m_ExcludeLayers":
                c.excludeLayers = field.GetAs<int>();
                return true;

            case "m_ProvidesContacts":
                c.providesContacts = field.GetAs<bool>();
                return true;
        }

        return false;
    }
}