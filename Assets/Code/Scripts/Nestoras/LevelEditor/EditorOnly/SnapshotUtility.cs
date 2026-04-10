#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

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

        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;

        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Quaternion quaternionValue;
        public Color colorValue;

        public int objectReferenceInstanceID;
        public int enumValueIndex;

        public object GetAsType(Type targetType)
        {
            return type switch
            {
                SerializedValueType.Boolean => boolValue,
                SerializedValueType.Integer => intValue,
                SerializedValueType.Float => floatValue,
                SerializedValueType.String => stringValue,
                SerializedValueType.Vector2 => vector2Value,
                SerializedValueType.Vector3 => vector3Value,
                SerializedValueType.Vector4 => vector4Value,
                SerializedValueType.Quaternion => quaternionValue,
                SerializedValueType.Color => colorValue,
                SerializedValueType.Enum => Enum.ToObject(targetType, enumValueIndex),
                SerializedValueType.ObjectReference => objectReferenceInstanceID,
                _ => null
            };
        }

        public bool Equals(FieldSnapshot other)
        {
            if (type != other.type) return false;
            return type switch
            {
                SerializedValueType.Boolean => boolValue == other.boolValue,
                SerializedValueType.Integer => intValue == other.intValue,
                SerializedValueType.Float => Mathf.Approximately(floatValue, other.floatValue),
                SerializedValueType.String => stringValue == other.stringValue,
                SerializedValueType.Vector2 => vector2Value == other.vector2Value,
                SerializedValueType.Vector3 => vector3Value == other.vector3Value,
                SerializedValueType.Vector4 => vector4Value == other.vector4Value,
                SerializedValueType.Quaternion => quaternionValue == other.quaternionValue,
                SerializedValueType.Color => colorValue == other.colorValue,
                SerializedValueType.ObjectReference => objectReferenceInstanceID == other.objectReferenceInstanceID,
                SerializedValueType.Enum => enumValueIndex == other.enumValueIndex,
                _ => false
            };
        }
    }
    [Serializable] public enum SerializedValueType
    {
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
    private static HashSet<SerializedPropertyType> compositeTypes = new HashSet<SerializedPropertyType>()
    {
        SerializedPropertyType.Vector2,
        SerializedPropertyType.Vector3,
        SerializedPropertyType.Vector4,
        SerializedPropertyType.Quaternion,
        SerializedPropertyType.Color
    };
    #endregion

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

                // Warn if change can't be recreated at runtime
                if (!ComponentApplierRegistry.IsFieldSupported(component.GetType(), iterator.propertyPath)) Debug.LogWarning($"Attempting to capture unsupported field: {component.GetType().Name}.{iterator.propertyPath}");
                fields.Add(CaptureField(iterator));

                previous = iterator.Copy();
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
                result.boolValue = property.boolValue;
                break;
            case SerializedPropertyType.Integer:
                result.type = SerializedValueType.Integer;
                result.intValue = property.intValue;
                break;
            case SerializedPropertyType.Float:
                result.type = SerializedValueType.Float;
                result.floatValue = property.floatValue;
                break;
            case SerializedPropertyType.String:
                result.type = SerializedValueType.String;
                result.stringValue = property.stringValue;
                break;
            case SerializedPropertyType.Vector2:
                result.type = SerializedValueType.Vector2;
                result.vector2Value = property.vector2Value;
                break;
            case SerializedPropertyType.Vector3:
                result.type = SerializedValueType.Vector3;
                result.vector3Value = property.vector3Value;
                break;
            case SerializedPropertyType.Vector4:
                result.type = SerializedValueType.Vector4;
                result.vector4Value = property.vector4Value;
                break;
            case SerializedPropertyType.Quaternion:
                result.type = SerializedValueType.Quaternion;
                result.quaternionValue = property.quaternionValue;
                break;
            case SerializedPropertyType.Color:
                result.type = SerializedValueType.Color;
                result.colorValue = property.colorValue;
                break;
            case SerializedPropertyType.ObjectReference:
                result.type = SerializedValueType.ObjectReference;
                result.objectReferenceInstanceID = property.objectReferenceInstanceIDValue;
                break;
            case SerializedPropertyType.Enum:
                result.type = SerializedValueType.Enum;
                result.enumValueIndex = property.enumValueIndex;
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
    #endregion
    #endregion

    #region Diff System
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

    public static LevelModification BuildLevelModification(DiffResult diff)
    {
        // GAMEOBJECTS

        LevelModification levelMod = ScriptableObject.CreateInstance<LevelModification>();
        
        // Keep entire snapshots for reconstruction
        levelMod.added = diff.added;
        levelMod.removed = diff.removed;

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
            levelMod.modified.Add(goMod);
        }
        return levelMod;
    }
    #endregion
}
#endif