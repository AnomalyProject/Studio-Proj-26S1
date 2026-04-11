#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ObjectGuidRegistry;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Utility script for populating and accessing the <see cref="ObjectGuidRegistry"/>.
/// </summary>
public static class ObjectGuidRegistryUtility
{
    private static ObjectGuidRegistry registry;
    private static string registryPath = "Assets/Resources/LevelEditor/ObjectGuidRegistry.asset";

    static ObjectGuidRegistryUtility()
    {
        registry = AssetDatabase.LoadAssetAtPath<ObjectGuidRegistry>(registryPath);
        if (registry == null) GenerateNewRegistry();
    }

    public static ObjectGuidRegistry GenerateNewRegistry()
    {
        registry = ScriptableObject.CreateInstance<ObjectGuidRegistry>();
        AssetDatabase.CreateAsset(registry, registryPath);
        return registry;
    }

    public static string GetOrCreateGuid(UnityEngine.Object obj)
    {
        if (obj == null) return null;
        foreach (Entry e in registry.entries) if (e.obj == obj) return e.guid;

        string guid = System.Guid.NewGuid().ToString();
        registry.entries.Add(new Entry()
        {
            guid = guid,
            obj = obj
        });
        EditorUtility.SetDirty(registry);
        return guid;
    }
}
#endif