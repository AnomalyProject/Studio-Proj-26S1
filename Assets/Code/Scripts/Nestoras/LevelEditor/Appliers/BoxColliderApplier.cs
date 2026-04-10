using System.Collections.Generic;
using System;
using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine;

[Preserve] // Avoid stripping type from build
public class BoxColliderApplier : IComponentApplier
{
    public Type TargetType => typeof(BoxCollider);
    public HashSet<string> supportedFields { get; } = new HashSet<string>()
    {
        "m_Center",
        "m_Size",
        "m_IsTrigger",
        "m_Material",
        "m_ContactOffset",
        "m_LayerOverridePriority",
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
                c.center = field.vector3Value;
                return true;

            case "m_Size":
                c.size = field.vector3Value;
                return true;

            case "m_IsTrigger":
                c.isTrigger = field.boolValue;
                return true;

            case "m_Material":
                c.sharedMaterial = ResolvePhysicMaterial(field);
                return true;

            case "m_ContactOffset":
                c.contactOffset = field.floatValue;
                return true;

            case "m_LayerOverridePriority":
                c.layerOverridePriority = field.intValue;
                return true;

            case "m_IncludeLayers":
                c.includeLayers = field.intValue;
                return true;

            case "m_ExcludeLayers":
                c.excludeLayers = field.intValue;
                return true;

            case "m_ProvidesContacts":
                c.providesContacts = field.boolValue;
                return true;
        }

        return false;
    }

    #region Helpers
    private PhysicsMaterial ResolvePhysicMaterial(FieldSnapshot field)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.EntityIdToObject(field.objectReferenceInstanceID) as PhysicsMaterial;
#else
    return null; // runtime requires GUID/asset registry
#endif
    }
    #endregion
}