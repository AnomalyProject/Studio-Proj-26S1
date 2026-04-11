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
                SerializedValueType.Float => GetAs<float>(),
                SerializedValueType.String => GetAs<string>(),
                SerializedValueType.Vector2 => GetAs<Vector2>(),
                SerializedValueType.Vector3 => GetAs<Vector3>(),
                SerializedValueType.Vector4 => GetAs<Vector4>(),
                SerializedValueType.Quaternion => GetAs<Quaternion>(),
                SerializedValueType.Color => GetAs<Color>(),
                SerializedValueType.Enum => Enum.ToObject(targetType, GetAs<int>()),
                SerializedValueType.ObjectReference => GetAs<string>(),
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
        Float,
        String,
        Vector2,
        Vector3,
        Vector4,
        Quaternion,
        Color,
        ObjectReference,
        Enum
    }
    #endregion

#if UNITY_EDITOR
    private static HashSet<SerializedPropertyType> compositeTypes = new HashSet<SerializedPropertyType>()
    {
        SerializedPropertyType.Vector2,
        SerializedPropertyType.Vector3,
        SerializedPropertyType.Vector4,
        SerializedPropertyType.Quaternion,
        SerializedPropertyType.Color
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
                        Debug.LogWarning($"Skipping unsupported field: {component.GetType().Name}.{iterator.propertyPath}");
                        fields.RemoveAt(fields.Count - 1);
                    }
                    // Warn if change can't be recreated at runtime (MonoBehaviour fields can be modified directly via reflections)
                    else if (component is not MonoBehaviour) Debug.LogWarning($"Captured field without registered applier: {component.GetType().Name}.{iterator.propertyPath}");
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
        FieldSnapshot result = new FieldSnapshot()
        {
            path = property.propertyPath,
        };

        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                result.type = SerializedValueType.Boolean;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<bool>() { value = property.boolValue });
                break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.ArraySize:
                result.type = SerializedValueType.Integer;
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
            case SerializedPropertyType.Enum:
                result.type = SerializedValueType.Enum;
                result.valueJson = JsonUtility.ToJson(new ValueWrapper<int>() { value = property.enumValueIndex });
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
                return property.intValue == default;
            case SerializedPropertyType.Float:
                return property.floatValue == default;
            case SerializedPropertyType.String:
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
            default: return false;
        };
    }
    #endregion
#endif
    #endregion

    #region Diff System
#if UNITY_EDITOR
    #region Structures
    public class DiffResult // Result container for hierarchy-level differences
    {
        public List<GameObjectSnapshot> added = new List<GameObjectSnapshot>();
        public List<GameObjectSnapshot> removed = new List<GameObjectSnapshot>();
        public List<(GameObjectSnapshot before, GameObjectSnapshot after)> changed = new List<(GameObjectSnapshot before, GameObjectSnapshot after)>();
    }
    public class GameObjectDiff // Result container for GameObject-level differences
    {
        public (string before, string after) parentGuid;
        public (bool before, bool after) isStatic;
        public (string before, string after) name;
        public (bool before, bool after) active;
        public (string before, string after) tag;
        public (int before, int after) layer;
        public List<ComponentSnapshot> added = new List<ComponentSnapshot>();
        public List<ComponentSnapshot> removed = new List<ComponentSnapshot>();
        public List<(ComponentSnapshot before, ComponentSnapshot after)> modified = new List<(ComponentSnapshot before, ComponentSnapshot after)>();
    }
    public class ComponentDiff // Result container for Component-level differences
    {
        public List<(FieldSnapshot before, FieldSnapshot after)> modified = new List<(FieldSnapshot before, FieldSnapshot after)>();
    }
    #endregion

    // Compares the GameObjects of a hierarchy's snapshots
    public static DiffResult Diff(List<GameObjectSnapshot> original, List<GameObjectSnapshot> modified)
    {
        DiffResult result = new DiffResult();

        Dictionary<string, GameObjectSnapshot> origMap = ToGameObjectMap(original);
        Dictionary<string, GameObjectSnapshot> modMap = ToGameObjectMap(modified);

        // Detect added objects
        foreach (KeyValuePair<string, GameObjectSnapshot> go in modMap) if (!origMap.ContainsKey(go.Key)) result.added.Add(go.Value);

        // Detect modified objects
        foreach (KeyValuePair<string, GameObjectSnapshot> go in origMap)
        {
            // Track removed objects
            if (!modMap.ContainsKey(go.Key))
            {
                result.removed.Add(go.Value);
                continue;
            }

            GameObjectSnapshot before = go.Value;
            GameObjectSnapshot after = modMap[go.Key];
            if (GameObjectHasChanged(before, after)) result.changed.Add((before, after));
        }

        return result;
    }
    private static bool GameObjectHasChanged(GameObjectSnapshot a, GameObjectSnapshot b)
    {
        // Detect non-component changes
        if (a.parentGuid != b.parentGuid || a.isStatic != b.isStatic || a.name != b.name || a.active != b.active || a.tag != b.tag || a.layer != b.layer) return true;

        // Delegate deeper comparison to component diff
        GameObjectDiff diff = GameObjectDiffByComponents(a.components, b.components);
        return diff.added.Count > 0 || diff.removed.Count > 0 || diff.modified.Count > 0;
    }

    // Converts GameObjectSnapshot list into a dictionary keyed by GUID
    private static Dictionary<string, GameObjectSnapshot> ToGameObjectMap(List<GameObjectSnapshot> list)
    {
        Dictionary<string, GameObjectSnapshot> map = new Dictionary<string, GameObjectSnapshot>();
        foreach (GameObjectSnapshot item in list) map[item.guid] = item;
        return map;
    }



    // Compares the components of a GameObject's snapshots
    private static GameObjectDiff GameObjectDiffByComponents(List<ComponentSnapshot> a, List<ComponentSnapshot> b)
    {
        GameObjectDiff result = new GameObjectDiff();

        Dictionary<string, ComponentSnapshot> mapA = ToComponentMap(a);
        Dictionary<string, ComponentSnapshot> mapB = ToComponentMap(b);

        // Detect added components
        foreach (KeyValuePair<string, ComponentSnapshot> comp in mapB) if (!mapA.ContainsKey(comp.Key)) result.added.Add(comp.Value);

        // Detect modified components
        foreach (KeyValuePair<string, ComponentSnapshot> comp in mapA)
        {
            // Track removed components
            if (!mapB.ContainsKey(comp.Key))
            {
                result.removed.Add(comp.Value);
                continue;
            }

            ComponentSnapshot compA = comp.Value;
            ComponentSnapshot compB = mapB[comp.Key];
            if (HasComponentChanged(compA, compB)) result.modified.Add((compA, compB));
        }

        return result;
    }
    private static bool HasComponentChanged(ComponentSnapshot a, ComponentSnapshot b)
    {
        if (a.enabled != b.enabled) return true;
        return ComponentDiffByFields(a.fields, b.fields).modified.Count > 0;
    }

    // Converts ComponentSnapshot list into a dictionary keyed by component type and ordinal, seperated by '#'
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



    // Compares the serialized fields of a component's snapshots
    private static ComponentDiff ComponentDiffByFields(List<FieldSnapshot> a, List<FieldSnapshot> b)
    {
        ComponentDiff result = new ComponentDiff();

        Dictionary<string, FieldSnapshot> mapA = a.ToDictionary(x => x.path);
        Dictionary<string, FieldSnapshot> mapB = b.ToDictionary(x => x.path);

        foreach (KeyValuePair<string, FieldSnapshot> field in mapA)
        {
            FieldSnapshot fieldA = field.Value;
            FieldSnapshot fieldB = mapB[field.Key];
            if (!fieldA.Equals(fieldB)) result.modified.Add((fieldA, fieldB));
        }

        return result;
    }
#endif
    #endregion

    #region Exporting
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
    public static LevelModification BuildLevelModification(DiffResult diff)
    {
        // GAMEOBJECTS

        LevelModification levelMod = ScriptableObject.CreateInstance<LevelModification>();
        
        // Keep entire snapshots for reconstruction
        levelMod.addedGameObjects = diff.added;
        levelMod.removedGameObjects = diff.removed;

        // Modified
        foreach ((GameObjectSnapshot objBefore, GameObjectSnapshot objAfter) in diff.changed)
        {
            GameObjectModification goMod = new GameObjectModification()
            {
                guid = objBefore.guid
            };

            goMod.oldParentGuid = objBefore.parentGuid;
            goMod.newParentGuid = objAfter.parentGuid;

            goMod.oldIsStatic = objBefore.isStatic;
            goMod.newIsStatic = objAfter.isStatic;

            goMod.oldName = objBefore.name;
            goMod.newName = objAfter.name;

            goMod.oldActive = objBefore.active;
            goMod.newActive = objAfter.active;

            goMod.oldTag = objBefore.tag;
            goMod.newTag = objAfter.tag;

            goMod.oldLayer = objBefore.layer;
            goMod.newLayer = objAfter.layer;
            
            // COMPONENTS

            GameObjectDiff componentDifferences = GameObjectDiffByComponents(objBefore.components, objAfter.components);

            // Keep entire snapshots for reconstruction
            goMod.addedComponents = componentDifferences.added;
            goMod.removedComponents = componentDifferences.removed;

            // Modified
            foreach((ComponentSnapshot compBefore, ComponentSnapshot compAfter) in componentDifferences.modified)
            {
                ComponentModification compMod = new ComponentModification()
                {
                    type = compBefore.type,
                    index = compBefore.index
                };

                compMod.oldEnabled = compBefore.enabled;
                compMod.newEnabled = compAfter.enabled;

                // FIELDS

                ComponentDiff fieldDifferences = ComponentDiffByFields(compBefore.fields, compAfter.fields);

                foreach ((FieldSnapshot fieldBefore, FieldSnapshot fieldAfter) in fieldDifferences.modified)
                {
                    if (!fieldBefore.Equals(fieldAfter))
                    {
                        FieldModification fieldMod = new FieldModification()
                        {
                            before = fieldBefore,
                            after = fieldAfter
                        };
                        compMod.fieldModifications.Add(fieldMod);
                    }
                }
                goMod.componentModifications.Add(compMod);
            }
            levelMod.gameObjectModifications.Add(goMod);
        }
        return levelMod;
    }
#endif
    #endregion
}