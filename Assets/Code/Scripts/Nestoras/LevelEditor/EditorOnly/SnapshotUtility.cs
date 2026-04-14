using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Utility script for capturing a snapshot of the in-editor state of an entire hierarchy of GameObjects,
/// Comparing snapshots to get all modified fields between them and their values,
/// and Exporting these <see cref="LevelModification"/>s as scriptable objects, so that they can be applied and reverted at runtime by <see cref="ModificationApplier"/>s.
/// </summary>
public static class SnapshotUtility
{
    #region Capturing
    #region Structures
    [Serializable] public class GameObjectSnapshot // Represents a full snapshot of a GameObject
    {
        // Key
        public string guid; // persists across edits

        public string parentGuid;
        public bool isStatic;
        public string name;
        public bool active;
        public string tag;
        public int layer;
        public List<ComponentSnapshot> components;
    }
    [Serializable] public class ComponentSnapshot // Represents a full snapshot of a single component on a GameObject
    {
        // Keys
        public string type;
        public int index; // Index among components of same type (for differentiating between duplicates)

        public bool enabled;
        public List<FieldSnapshot> fields;
    }
    [Serializable] public class FieldSnapshot // Represents a full snapshot of a single SerializedProperty on a component
    {
        // Key
        public string path;

        public SerializedValueType type;
        public string valueJson;

        public bool Equals(FieldSnapshot other)
        {
            if (type != other.type) return false;
            return valueJson == other.valueJson;
        }
        public object GetAsType(Type targetType)
        {
            return type switch
            {
                SerializedValueType.Boolean => GetAs<bool>(),
                SerializedValueType.Integer => GetAs<int>(),
                SerializedValueType.ArraySize => GetAs<int>(),
                SerializedValueType.Float => GetAs<float>(),
                SerializedValueType.String => GetAs<string>(),
                SerializedValueType.Vector2 => GetAs<Vector2>(),
                SerializedValueType.Vector3 => GetAs<Vector3>(),
                SerializedValueType.Vector4 => GetAs<Vector4>(),
                SerializedValueType.Quaternion => GetAs<Quaternion>(),
                SerializedValueType.Color => GetAs<Color>(),
                SerializedValueType.Enum => Enum.ToObject(targetType, GetAs<int>()),
                SerializedValueType.ObjectReference => GetAs<string>(),
                SerializedValueType.AnimationCurve => GetAs<AnimationCurve>(),
                SerializedValueType.Gradient => GetAs<Gradient>(),
                SerializedValueType.Rect => GetAs<Rect>(),
                SerializedValueType.Bounds => GetAs<Bounds>(),
                SerializedValueType.Vector2Int => GetAs<Vector2Int>(),
                SerializedValueType.Vector3Int => GetAs<Vector3Int>(),
                SerializedValueType.RectInt => GetAs<RectInt>(),
                SerializedValueType.BoundsInt => GetAs<BoundsInt>(),
                SerializedValueType.LayerMask => GetAs<int>(),
                SerializedValueType.Character => GetAs<string>(),
                _ => null
            };
        }
        public UnityEngine.Object GetAsObject()
        {
            string guid = GetAs<string>();
            if (string.IsNullOrEmpty(guid)) return null;
            return ComponentApplierRegistry.objectGuidRegistry.Get(guid);
        }
        public T GetAs<T>()
        {
            if (string.IsNullOrEmpty(valueJson)) return default;
            ValueWrapper<T> wrapper = JsonUtility.FromJson<ValueWrapper<T>>(valueJson);
            if (wrapper == null) return default;
            return wrapper.value;
        }
    }
    [Serializable] public class ValueWrapper<T>
    {
        public T value;
    }
    [Serializable] public enum SerializedValueType
    {
        None,
        Boolean,
        Integer,
        ArraySize,
        Float,
        String,
        Vector2,
        Vector3,
        Vector4,
        Quaternion,
        Color,
        ObjectReference,
        Enum,
        AnimationCurve,
        Gradient,
        Rect,
        Bounds,
        Vector2Int,
        Vector3Int,
        RectInt,
        BoundsInt,
        LayerMask,
        Character,
    }
    #endregion

#if UNITY_EDITOR
    private static HashSet<SerializedPropertyType> compositeTypes = new HashSet<SerializedPropertyType>()
    {
        SerializedPropertyType.Vector2,
        SerializedPropertyType.Vector2Int,
        SerializedPropertyType.Vector3,
        SerializedPropertyType.Vector3Int,
        SerializedPropertyType.Vector4,
        SerializedPropertyType.Quaternion,
        SerializedPropertyType.Color,
        SerializedPropertyType.Rect,
        SerializedPropertyType.RectInt,
        SerializedPropertyType.Bounds,
        SerializedPropertyType.BoundsInt,
    };

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
        GameObjectSnapshot snapshot = new GameObjectSnapshot()
        {
            guid = id.guid,
            parentGuid = t.parent?.GetComponent<SnapshotID>()?.guid,
            isStatic = go.isStatic,
            name = go.name,
            active = go.activeSelf,
            tag = go.tag,
            layer = go.layer,
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

        Component[] components = go.GetComponents<Component>();
        Dictionary<string, int> typeCounts = new Dictionary<string, int>();

        foreach (Component component in components)
        {
            if (component == null) continue;

            string type = component.GetType().AssemblyQualifiedName;

            if (!typeCounts.ContainsKey(type)) typeCounts[type] = 0;
            int index = typeCounts[type]++;

            SerializedObject so = new SerializedObject(component);
            SerializedProperty iterator = so.GetIterator();
            SerializedProperty previous = null;

            List<FieldSnapshot> fields = new List<FieldSnapshot>();

            while (iterator.NextVisible(true))
            {
                if (iterator.name == "m_Script") continue;

                // Skip ignored fields based on registry
                if (ComponentApplierRegistry.IsFieldIgnored(component.GetType(), iterator.propertyPath)) continue;

                // Skip x,y,z fields of composite types like Vector3, since we already store the entire Vector3
                if (previous != null && iterator.depth > previous.depth && compositeTypes.Contains(previous.propertyType)) continue;
                previous = iterator.Copy();

                // Skip default values
                //if (HasDefaultValue(iterator)) continue;

                fields.Add(CaptureField(iterator));

                if (!ComponentApplierRegistry.IsFieldSupported(component.GetType(), iterator.propertyPath))
                {
                    // Warn if field isn't serializable
                    if (fields.Last().type == SerializedValueType.None)
                    {
                        // Arrays / lists are handled either by reflections or by custom appliers. If it has visible children in the inspector, it's probably just a foldout menu.
                        if (!iterator.isArray && !iterator.hasVisibleChildren) Debug.LogWarning($"Skipping unsupported field: {component.GetType().Name}.{iterator.propertyPath}");
                        fields.RemoveAt(fields.Count - 1);
                    }
                    // Warn if change can't be recreated at runtime (MonoBehaviour fields can be modified directly via reflections)
                    else if (component is not MonoBehaviour) ModificationEditorWindow.capturedNativeFieldWithoutApplier = true;
                }
            }

            result.Add(new ComponentSnapshot()
            {
                type = type,
                index = index,
                enabled = GetComponentEnabled(component),
                fields = fields
            });
        }

        return result;
    }
    
    // Captures all serialized properties on a Component
    private static FieldSnapshot CaptureField(SerializedProperty property)
    {
        FieldSnapshot result = new FieldSnapshot() { path = property.propertyPath};
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                result.type = SerializedValueType.Boolean;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<bool>() { value = property.boolValue });
                break;
            case SerializedPropertyType.Integer:
                result.type = SerializedValueType.Integer;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<int>() { value = property.intValue });
                break;
            case SerializedPropertyType.ArraySize:
                result.type = SerializedValueType.ArraySize;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<int>() { value = property.intValue });
                break;
            case SerializedPropertyType.Float:
                result.type = SerializedValueType.Float;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<float>() { value = property.floatValue });
                break;
            case SerializedPropertyType.String:
                result.type = SerializedValueType.String;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<string>() { value = property.stringValue });
                break;
            case SerializedPropertyType.Vector2:
                result.type = SerializedValueType.Vector2;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Vector2>() { value = property.vector2Value });
                break;
            case SerializedPropertyType.Vector3:
                result.type = SerializedValueType.Vector3;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Vector3>() { value = property.vector3Value });
                break;
            case SerializedPropertyType.Vector4:
                result.type = SerializedValueType.Vector4;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Vector4>() { value = property.vector4Value });
                break;
            case SerializedPropertyType.Quaternion:
                result.type = SerializedValueType.Quaternion;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Quaternion>() { value = property.quaternionValue });
                break;
            case SerializedPropertyType.Color:
                result.type = SerializedValueType.Color;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Color>() { value = property.colorValue });
                break;
            case SerializedPropertyType.ObjectReference:
                result.type = SerializedValueType.ObjectReference;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<string>() { value = ObjectGuidRegistryUtility.GetOrCreateGuid(property.objectReferenceValue) });
                break;
            case SerializedPropertyType.ExposedReference:
                // Treat exposed references like object references by storing referenced object's guid
                result.type = SerializedValueType.ObjectReference;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<string>() { value = ObjectGuidRegistryUtility.GetOrCreateGuid(property.objectReferenceValue) });
                break;
            case SerializedPropertyType.Enum:
                result.type = SerializedValueType.Enum;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<int>() { value = property.enumValueIndex });
                break;
            case SerializedPropertyType.AnimationCurve:
                result.type = SerializedValueType.AnimationCurve;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<AnimationCurve>() { value = property.animationCurveValue });
                break;
            case SerializedPropertyType.Gradient:
                result.type = SerializedValueType.Gradient;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Gradient>() { value = property.gradientValue });
                break;
            case SerializedPropertyType.Rect:
                result.type = SerializedValueType.Rect;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Rect>() { value = property.rectValue });
                break;
            case SerializedPropertyType.Bounds:
                result.type = SerializedValueType.Bounds;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Bounds>() { value = property.boundsValue });
                break;
            case SerializedPropertyType.LayerMask:
                result.type = SerializedValueType.LayerMask;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<int>() { value = property.intValue });
                break;
            case SerializedPropertyType.Character:
                result.type = SerializedValueType.Character;
                // Character often exposed as a single-char string in SerializedProperty
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<string>() { value = property.stringValue });
                break;
            case SerializedPropertyType.Vector2Int:
                result.type = SerializedValueType.Vector2Int;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Vector2Int>() { value = property.vector2IntValue });
                break;
            case SerializedPropertyType.Vector3Int:
                result.type = SerializedValueType.Vector3Int;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<Vector3Int>() { value = property.vector3IntValue });
                break;
            case SerializedPropertyType.RectInt:
                result.type = SerializedValueType.RectInt;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<RectInt>() { value = property.rectIntValue });
                break;
            case SerializedPropertyType.BoundsInt:
                result.type = SerializedValueType.BoundsInt;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<BoundsInt>() { value = property.boundsIntValue });
                break;
            default:
                result.type = SerializedValueType.None;
                break;
        };
        return result;
    }

    #region Helpers
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
    private static bool GetComponentEnabled(Component c)
    {
        if (c is Behaviour behaviour) return behaviour.enabled; // Behaviour covers MonoBehaviour, scripts, etc.
        if (c is Renderer renderer) return renderer.enabled; // Renderers
        if (c is Collider collider) return collider.enabled; // Colliders
        return true; // Components without enabled state are always considered enabled
    }
    private static bool HasDefaultValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return property.boolValue == default;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.LayerMask:
                return property.intValue == default;
            case SerializedPropertyType.ArraySize:
                return property.arraySize == default;
            case SerializedPropertyType.Float:
                return property.floatValue == default;
            case SerializedPropertyType.String:
            case SerializedPropertyType.Character:
                return property.stringValue == default;
            case SerializedPropertyType.Vector2:
                return property.vector2Value == default;
            case SerializedPropertyType.Vector3:
                return property.vector3Value == default;
            case SerializedPropertyType.Vector4:
                return property.vector4Value == default;
            case SerializedPropertyType.Quaternion:
                return property.quaternionValue == default;
            case SerializedPropertyType.Color:
                return property.colorValue == default;
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == default;
            case SerializedPropertyType.Enum:
                return property.enumValueIndex == default;
            case SerializedPropertyType.AnimationCurve:
                return property.animationCurveValue == default;
            case SerializedPropertyType.Gradient:
                return property.gradientValue == default;
            case SerializedPropertyType.Rect:
                return property.rectValue == default;
            case SerializedPropertyType.Bounds:
                return property.boundsValue == default;
            case SerializedPropertyType.Vector2Int:
                return property.vector2IntValue == default;
            case SerializedPropertyType.Vector3Int:
                return property.vector3IntValue == default;
            case SerializedPropertyType.RectInt:
                return property.rectIntValue == default;
            case SerializedPropertyType.BoundsInt:
                return property.boundsIntValue == default;
            case SerializedPropertyType.ExposedReference:
                return property.exposedReferenceValue == default;
            default: return false;
        };
    }
    #endregion
#endif
    #endregion

    #region Diff System
    #region Structures
    [Serializable] public class GameObjectModification // A group of changes regarding a specific GameObject
    {
        // Key
        public string guid;

        public string oldParentGuid;
        public string newParentGuid;

        public bool oldIsStatic;
        public bool newIsStatic;

        public string oldName;
        public string newName;

        public bool oldActive;
        public bool newActive;

        public string oldTag;
        public string newTag;

        public int oldLayer;
        public int newLayer;

        public List<ComponentSnapshot> addedComponents = new List<ComponentSnapshot>();
        public List<ComponentSnapshot> removedComponents = new List<ComponentSnapshot>();

        public List<ComponentModification> componentModifications = new List<ComponentModification>();
    }
    [Serializable] public class ComponentModification // A group of changes regarding a specific Component
    {
        // Keys
        public string type;
        public int index;

        public bool oldEnabled;
        public bool newEnabled;
        public List<FieldModification> fieldModifications = new List<FieldModification>();
    }
    [Serializable] public class FieldModification // A group of changes regarding a specific SerializedProperty
    {
        public FieldSnapshot before;
        public FieldSnapshot after;
    }
    #endregion
#if UNITY_EDITOR

    // Compares the GameObjects of a hierarchy's snapshots
    public static LevelModification Diff(List<GameObjectSnapshot> original, List<GameObjectSnapshot> modified)
    {
        LevelModification result = ScriptableObject.CreateInstance<LevelModification>();

        Dictionary<string, GameObjectSnapshot> origMap = ToGameObjectMap(original);
        Dictionary<string, GameObjectSnapshot> modMap = ToGameObjectMap(modified);

        // Detect added objects
        foreach (KeyValuePair<string, GameObjectSnapshot> go in modMap) if (!origMap.ContainsKey(go.Key)) result.addedGameObjects.Add(go.Value);

        // Detect modified objects
        foreach (KeyValuePair<string, GameObjectSnapshot> go in origMap)
        {
            // Track removed objects
            if (!modMap.ContainsKey(go.Key))
            {
                result.removedGameObjects.Add(go.Value);
                continue;
            }

            GameObjectSnapshot before = go.Value;
            GameObjectSnapshot after = modMap[go.Key];

            // Component-level diff for this GameObject
            GameObjectModification goModification = new GameObjectModification()
            {
                guid = before.guid,
                oldParentGuid = before.parentGuid,
                newParentGuid = after.parentGuid,
                oldIsStatic = before.isStatic,
                newIsStatic = after.isStatic,
                oldName = before.name,
                newName = after.name,
                oldActive = before.active,
                newActive = after.active,
                oldTag = before.tag,
                newTag = after.tag,
                oldLayer = before.layer,
                newLayer = after.layer,
            };

            Dictionary<string, ComponentSnapshot> mapA = ToComponentMap(before.components);
            Dictionary<string, ComponentSnapshot> mapB = ToComponentMap(after.components);

            // Detect added components
            foreach (KeyValuePair<string, ComponentSnapshot> comp in mapB) if (!mapA.ContainsKey(comp.Key)) goModification.addedComponents.Add(comp.Value);

            // Detect modified components
            foreach (KeyValuePair<string, ComponentSnapshot> comp in mapA)
            {
                // Track removed components
                if (!mapB.ContainsKey(comp.Key))
                {
                    goModification.removedComponents.Add(comp.Value);
                    continue;
                }

                ComponentSnapshot compA = comp.Value;
                ComponentSnapshot compB = mapB[comp.Key];


                // Field-level diff for this component
                ComponentModification componentModification = new ComponentModification();

                Dictionary<string, FieldSnapshot> compMapA = compA.fields.ToDictionary(x => x.path);
                Dictionary<string, FieldSnapshot> compMapB = compB.fields.ToDictionary(x => x.path);

                foreach (KeyValuePair<string, FieldSnapshot> field in compMapA)
                {
                    FieldSnapshot fieldA = field.Value;
                    FieldSnapshot fieldB = compMapB[field.Key];

                    // Skip field if values are the same
                    if (fieldA.Equals(fieldB)) continue;

                    // Add field to ComponentModification if change detected
                    componentModification.fieldModifications.Add(new FieldModification()
                    {
                        before = fieldA,
                        after = fieldB
                    });
                }

                // Skip component if all properties are the same
                if (compA.enabled == compB.enabled &&
                    componentModification.fieldModifications.Count == 0)
                    continue;

                // Add component to GameObjectModification if any changes detected
                goModification.componentModifications.Add(new ComponentModification()
                {
                    type = compA.type,
                    index = compA.index,
                    oldEnabled = compA.enabled,
                    newEnabled = compB.enabled,
                    fieldModifications = componentModification.fieldModifications
                });
            }

            // Skip GameObject if all properties are the same
            if (before.parentGuid == after.parentGuid &&
                before.isStatic == after.isStatic &&
                before.name == after.name &&
                before.active == after.active &&
                before.tag == after.tag &&
                before.layer == after.layer &&
                goModification.addedComponents.Count == 0 &&
                goModification.removedComponents.Count == 0 &&
                goModification.componentModifications.Count == 0)
                continue;

            // Add GameObject to modified list if any changes detected
            result.gameObjectModifications.Add(goModification);
        }

        // Return null if no changes detected
        if (result.addedGameObjects.Count == 0 && result.removedGameObjects.Count == 0 && result.gameObjectModifications.Count == 0) return null;
        return result;
    }

    // Converts GameObjectSnapshot list into a dictionary keyed by GUID
    private static Dictionary<string, GameObjectSnapshot> ToGameObjectMap(List<GameObjectSnapshot> list)
    {
        Dictionary<string, GameObjectSnapshot> map = new Dictionary<string, GameObjectSnapshot>();
        foreach (GameObjectSnapshot item in list) map[item.guid] = item;
        return map;
    }

    // Converts ComponentSnapshot list into a dictionary keyed by component type and ordinal to distinguish duplicates
    private static Dictionary<string, ComponentSnapshot> ToComponentMap(List<ComponentSnapshot> list)
    {
        Dictionary<string, ComponentSnapshot> map = new Dictionary<string, ComponentSnapshot>();
        foreach (ComponentSnapshot component in list) map[component.type + component.index] = component;
        return map;
    }

#endif
    #endregion
}