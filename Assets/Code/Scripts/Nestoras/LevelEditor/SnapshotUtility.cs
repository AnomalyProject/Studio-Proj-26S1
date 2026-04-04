#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

public static class SnapshotUtility
{
    #region Structures
    [Serializable] // Represents a full snapshot of a GameObject
    public class GameObjectSnapshot
    {
        public string guid;     // Stable unique ID (persists across edits)
        public string path;     // Hierarchy path (Root/Child/SubChild)
        public string name;     // GameObject name
        public bool active;     // ActiveSelf state
        public List<ComponentSnapshot> components; // All components + their serialized data
    }

    [Serializable] // Represents a snapshot of a single component on a GameObject
    public class ComponentSnapshot
    {
        public string type;     // Fully qualified type name (used for matching)
        public int index;       // Index among components of same type (important for duplicates)
        public bool enabled;    // Whether the component is enabled (if applicable)
        public Dictionary<string, TypedValue> properties; // Serialized properties of the component (propertyPath -> value)
    }
    
    [Serializable] // Abstaraction layer to handle SerializedProperty values as objects while keeping the ability to interpret them
    public class TypedValue
    {
        public SerializedPropertyType type; // Type of the original SerializedProperty
        public object value;                // Stored as object to handle everything in a uniform way
    }

    // Result container for GameObject-level differences
    public class DiffResult
    {
        public List<GameObjectSnapshot> added = new List<GameObjectSnapshot>();
        public List<GameObjectSnapshot> removed = new List<GameObjectSnapshot>();
        // Tuple of (before, after) for modified objects
        public List<(GameObjectSnapshot before, GameObjectSnapshot after)> changed = new List<(GameObjectSnapshot before, GameObjectSnapshot after)>();
    }

    // Result container for component-level differences
    public class ComponentDiff
    {
        public List<ComponentSnapshot> added = new List<ComponentSnapshot>();
        public List<ComponentSnapshot> removed = new List<ComponentSnapshot>();
        public List<(ComponentSnapshot before, ComponentSnapshot after)> modified = new List<(ComponentSnapshot before, ComponentSnapshot after)>();
    }
    #endregion

    #region Capturing
    // Captures a full hierarchy snapshot
    public static List<GameObjectSnapshot> Capture(GameObject root)
    {
        List<GameObjectSnapshot> list = new List<GameObjectSnapshot>();
        Traverse(root.transform, list); // Recursively traverse hierarchy starting from root
        return list;
    }

    // Recursively walks the transform hierarchy and populates list
    private static void Traverse(Transform t, List<GameObjectSnapshot> list)
    {
        GameObject go = t.gameObject;
        SnapshotID id = GetOrAddID(go); // Ensure object has a persistent GUID

        // Build snapshot for this GameObject
        GameObjectSnapshot snapshot = new GameObjectSnapshot
        {
            guid = id.guid,
            path = GetTransformPath(t),
            name = go.name,
            active = go.activeSelf,
            components = CaptureComponents(go) // Capture all components and their serialized properties
        };
        list.Add(snapshot);

        // Recurse into children
        foreach (Transform child in t) Traverse(child, list);
    }

    // Captures all components on a GameObject
    private static List<ComponentSnapshot> CaptureComponents(GameObject go)
    {
        List<ComponentSnapshot> result = new List<ComponentSnapshot>();

        // Get all attached components
        Component[] components = go.GetComponents<Component>();

        // Tracks duplicate component types
        Dictionary<string, int> typeCounts = new Dictionary<string, int>();

        foreach (Component component in components)
        {
            if (component == null) continue;

            // Use fully qualified type name for uniqueness
            string type = component.GetType().AssemblyQualifiedName;

            // Count multiple components of same type
            if (!typeCounts.ContainsKey(type)) typeCounts[type] = 0;
            int index = typeCounts[type]++;

            // Use Unity serialization to inspect component fields/properties
            SerializedObject so = new SerializedObject(component);
            SerializedProperty iterator = so.GetIterator();

            Dictionary<string, TypedValue> properties = new Dictionary<string, TypedValue>();

            // Iterate through all visible serialized fields
            while (iterator.NextVisible(true))
            {
                if (iterator.name == "m_Script") continue; // Skip internal script reference
                properties[iterator.propertyPath] = GetValue(iterator);
            }

            result.Add(new ComponentSnapshot
            {
                type = type,
                index = index,
                enabled = GetComponentEnabled(component),
                properties = properties
            });
        }

        return result;
    }

    #region Helpers
    private static TypedValue GetValue(SerializedProperty p)
    {
        TypedValue result = new TypedValue();
        result.type = p.propertyType;

        result.value = p.propertyType switch
        {
            SerializedPropertyType.Integer => p.intValue,
            SerializedPropertyType.Boolean => p.boolValue,
            SerializedPropertyType.Float => p.floatValue,
            SerializedPropertyType.String => p.stringValue,
            SerializedPropertyType.Vector2 => p.vector2Value,
            SerializedPropertyType.Vector3 => p.vector3Value,
            SerializedPropertyType.Vector4 => p.vector4Value,
            SerializedPropertyType.Quaternion => p.quaternionValue,
            SerializedPropertyType.Color => p.colorValue,
            SerializedPropertyType.ObjectReference => p.objectReferenceInstanceIDValue,
            SerializedPropertyType.Enum => p.enumValueIndex,
            _ => null
        };

        return result;
    }

    // Determines if a component has an "enabled" state
    private static bool GetComponentEnabled(Component c)
    {
        if (c is Behaviour behaviour) return behaviour.enabled; // Behaviour covers MonoBehaviour, scripts, etc.
        if (c is Renderer renderer) return renderer.enabled; // Renderers
        if (c is Collider collider) return collider.enabled; // Colliders
        return true; // Components without enabled state are always considered enabled
    }

    // Builds a hierarchy path like Root/Child/GrandChild
    private static string GetTransformPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetTransformPath(t.parent) + "/" + t.name;
    }

    private static SnapshotID GetOrAddID(GameObject go)
    {
        SnapshotID id = go.GetComponent<SnapshotID>();
        if (id == null) // Add if missing
        {
            id = go.AddComponent<SnapshotID>();
            id.guid = Guid.NewGuid().ToString();
        }
        return id;
    }
    #endregion
    #endregion

    #region Diff System
    // Compares two snapshots of a hierarchy
    public static DiffResult Diff(List<GameObjectSnapshot> original, List<GameObjectSnapshot> modified)
    {
        DiffResult result = new DiffResult();

        Dictionary<string, GameObjectSnapshot> origMap = ToMap(original);
        Dictionary<string, GameObjectSnapshot> modMap = ToMap(modified);

        // Detect added objects
        foreach (KeyValuePair<string, GameObjectSnapshot> kv in modMap)
        {
            if (!origMap.ContainsKey(kv.Key)) result.added.Add(kv.Value);
        }

        // Detect removed objects
        foreach (KeyValuePair<string, GameObjectSnapshot> kv in origMap)
        {
            if (!modMap.ContainsKey(kv.Key)) result.removed.Add(kv.Value);
        }

        // Detect modified objects (exist in both but changed)
        foreach (KeyValuePair<string, GameObjectSnapshot> kv in origMap)
        {
            if (!modMap.ContainsKey(kv.Key)) continue;

            GameObjectSnapshot before = kv.Value;
            GameObjectSnapshot after = modMap[kv.Key];

            if (HasChanged(before, after)) result.changed.Add((before, after));
        }

        return result;
    }

    // Checks if a GameObject snapshot differs
    private static bool HasChanged(GameObjectSnapshot a, GameObjectSnapshot b)
    {
        // Active state change counts as a change
        if (a.active != b.active) return true;

        // Delegate deeper comparison to component diff
        ComponentDiff compDiff = DiffComponents(a.components, b.components);

        return compDiff.added.Count > 0 || compDiff.removed.Count > 0 || compDiff.modified.Count > 0;
    }

    // Converts snapshot list into a dictionary keyed by GUID
    private static Dictionary<string, GameObjectSnapshot> ToMap(List<GameObjectSnapshot> list)
    {
        Dictionary<string, GameObjectSnapshot> map = new Dictionary<string, GameObjectSnapshot>();
        foreach (GameObjectSnapshot item in list) map[item.guid] = item;
        return map;
    }

    // Compares components between two snapshots
    public static ComponentDiff DiffComponents(List<ComponentSnapshot> a, List<ComponentSnapshot> b)
    {
        ComponentDiff result = new ComponentDiff();

        Dictionary<string, ComponentSnapshot> mapA = ToComponentMap(a);
        Dictionary<string, ComponentSnapshot> mapB = ToComponentMap(b);

        // Components that exist only in B
        foreach (KeyValuePair<string, ComponentSnapshot> kv in mapB) if (!mapA.ContainsKey(kv.Key)) result.added.Add(kv.Value);

        // Components that exist only in A
        foreach (KeyValuePair<string, ComponentSnapshot> kv in mapA) if (!mapB.ContainsKey(kv.Key)) result.removed.Add(kv.Value);

        // Components present in both -> check for modifications
        foreach (KeyValuePair<string, ComponentSnapshot> kv in mapA)
        {
            if (!mapB.ContainsKey(kv.Key)) continue;

            ComponentSnapshot compA = kv.Value;
            ComponentSnapshot compB = mapB[kv.Key];

            if (HasComponentChanged(compA, compB)) result.modified.Add((compA, compB));
        }

        return result;
    }

    // Checks whether a component changed in any meaningful way
    private static bool HasComponentChanged(ComponentSnapshot a, ComponentSnapshot b)
    {
        if (a.enabled != b.enabled) return true; // Enabled state differs

        // Compare all serialized properties
        foreach (KeyValuePair<string, TypedValue> kv in a.properties)
        {
            if (!b.properties.TryGetValue(kv.Key, out TypedValue val)) return true;
            if (!AreValuesEqual(kv.Value, val)) return true;
        }
        return false;
    }

    // Converts component list into a lookup dictionary
    private static Dictionary<string, ComponentSnapshot> ToComponentMap(List<ComponentSnapshot> list)
    {
        Dictionary<string, ComponentSnapshot> map = new Dictionary<string, ComponentSnapshot>();
        foreach (ComponentSnapshot component in list)
        {
            // Key combines type + index to distinguish duplicates
            string key = component.type + "#" + component.index;
            map[key] = component;
        }
        return map;
    }

    private static bool AreValuesEqual(TypedValue a, TypedValue b)
    {
        // Different types = not equal
        if (a.type != b.type) return false;

        // Compare based on type
        return a.type switch
        {
            SerializedPropertyType.Integer => (int)a.value == (int)b.value,
            SerializedPropertyType.Boolean => (bool)a.value == (bool)b.value,
            SerializedPropertyType.Float => Mathf.Approximately((float)a.value, (float)b.value),
            SerializedPropertyType.String => (string)a.value == (string)b.value,
            SerializedPropertyType.Vector2 => (Vector2)a.value == (Vector2)b.value,
            SerializedPropertyType.Vector3 => (Vector3)a.value == (Vector3)b.value,
            SerializedPropertyType.Vector4 => (Vector4)a.value == (Vector4)b.value,
            SerializedPropertyType.Quaternion => (Quaternion)a.value == (Quaternion)b.value,
            SerializedPropertyType.Color => (Color)a.value == (Color)b.value,
            SerializedPropertyType.ObjectReference => (int)a.value == (int)b.value,
            SerializedPropertyType.Enum => (int)a.value == (int)b.value,
            _ => Equals(a.value, b.value)
        };
    }
    #endregion
}
#endif