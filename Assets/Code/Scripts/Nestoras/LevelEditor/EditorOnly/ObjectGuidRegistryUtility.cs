#if UNITY_EDITOR
using static ObjectGuidRegistry;
using static SnapshotUtility;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Utility script for populating and accessing the <see cref="ObjectGuidRegistry"/>.
/// </summary>
public static class ObjectGuidRegistryUtility
{
    public static Texture2D registryIcon;
    private static ObjectGuidRegistry registry;
    private static string registryPath = "Assets/Resources/LevelEditor/ObjectGuidRegistry.asset";

    [InitializeOnLoadMethod()]
    private static void FetchIcon() => registryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Code/Scripts/Nestoras/LevelEditor/registry.png");

    private static ObjectGuidRegistry GetRegistry()
    {
        if (registry != null) return registry;
        registry = AssetDatabase.LoadAssetAtPath<ObjectGuidRegistry>(registryPath) ?? GenerateNewRegistry();
        return registry;
    }

    public static ObjectGuidRegistry GenerateNewRegistry()
    {
        registry = ScriptableObject.CreateInstance<ObjectGuidRegistry>();
        AssetDatabase.CreateAsset(registry, registryPath);
        EditorGUIUtility.SetIconForObject(registry, registryIcon);
        ComponentApplierRegistry.SetRegistry(registry);
        return registry;
    }

    public static string GetOrCreateGuid(Object obj)
    {
        if (registry == null) registry = GetRegistry();

        if (obj == null) return null;
        foreach (Entry entry in registry.entries) if (entry.obj == obj) return entry.guid;

        string guid = System.Guid.NewGuid().ToString();
        registry.entries.Add(new Entry()
        {
            guid = guid,
            obj = obj
        });
        EditorUtility.SetDirty(registry);
        return guid;
    }

    public static void CleanRegistry()
    {
        if (registry == null) registry = GetRegistry();

        // Create a buffer to keep track of which registry entries are actually used in the level modifications.
        Dictionary<string, bool> entryUsageBuffer = registry.entries.ToDictionary(e => e.guid, e => false);

        // Recursively check all level modifications for any references to the registry entries, and mark them as used in the buffer.
        foreach (LevelModification levelModification in AssetDatabase.FindAssets("t:LevelModification").Select(guid => AssetDatabase.LoadAssetAtPath<LevelModification>(AssetDatabase.GUIDToAssetPath(guid))))
        {
            foreach (GameObjectSnapshot snapshot in levelModification.addedGameObjects) foreach (ComponentSnapshot component in snapshot.components) foreach (FieldSnapshot field in component.fields) if (field.type == SerializedValueType.ObjectReference) entryUsageBuffer[field.GetAs<string>()] = true;
            foreach (GameObjectSnapshot snapshot in levelModification.removedGameObjects) foreach (ComponentSnapshot component in snapshot.components) foreach (FieldSnapshot field in component.fields) if (field.type == SerializedValueType.ObjectReference) entryUsageBuffer[field.GetAs<string>()] = true;
            foreach (GameObjectModification modification in levelModification.gameObjectModifications)
            {
                foreach (ComponentSnapshot component in modification.addedComponents) foreach (FieldSnapshot field in component.fields) if (field.type == SerializedValueType.ObjectReference) entryUsageBuffer[field.GetAs<string>()] = true;
                foreach (ComponentSnapshot component in modification.removedComponents) foreach (FieldSnapshot field in component.fields) if (field.type == SerializedValueType.ObjectReference) entryUsageBuffer[field.GetAs<string>()] = true;

                foreach (ComponentModification component in modification.componentModifications)
                {
                    foreach (FieldModification field in component.fieldModifications)
                    {
                        if (field.before.type == SerializedValueType.ObjectReference)
                        {
                            entryUsageBuffer[field.before.GetAs<string>()] = true;
                            entryUsageBuffer[field.after.GetAs<string>()] = true;
                        }
                    }
                }
            }
        }

        int entries = registry.entries.Count;

        // Remvove unused entries from the registry.
        for (int i = registry.entries.Count - 1; i >= 0; i--) if (!entryUsageBuffer[registry.entries[i].guid]) registry.entries.RemoveAt(i);
        EditorUtility.SetDirty(registry);

        Debug.Log($"Cleaned ObjectGUIDRegistry. Removed {entries - registry.entries.Count} unused entries.");
    }
}
#endif