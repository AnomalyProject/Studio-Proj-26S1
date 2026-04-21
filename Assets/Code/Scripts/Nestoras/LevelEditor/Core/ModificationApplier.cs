using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System;
using static SnapshotUtility;
using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Script that handles applying and reverting level modifications.
/// Will delegate value application to <see cref="ComponentApplierRegistry"/> when possible, otherwise will attempt to apply changes using reflections (for custom components).
/// </summary>
[ExecuteAlways]
public class ModificationApplier : MonoBehaviour
{
    public LevelModification levelModification;
    [SerializeField] [HideInInspector] private bool applied;

    [Tooltip("Called after modification is applied.")]
    public UnityEvent onEnable;
    [Tooltip("Called before modification is reverted.")]
    public UnityEvent onDisable;

    #region Triggers
    private void OnEnable()
    {
        if (applied) return;
        applied = true;

        onEnable?.Invoke();
        Apply();
    }

    private void OnDisable()
    {
        if (!applied) return;
        applied = false;

        onDisable?.Invoke();
        Revert();
    }
    #endregion

    #region Core Logic
    private void Apply()
    {
        if (levelModification == null) return;

        // Additions
        foreach (GameObjectSnapshot goToAdd in levelModification.addedGameObjects)
        {
            GameObject parent = FindByGuid(goToAdd.parentGuid);
            if (parent == null) continue;

            GameObject go = new GameObject(goToAdd.name);
            go.transform.SetParent(parent.transform);

            go.isStatic = goToAdd.isStatic;
            go.SetActive(goToAdd.active);
            go.tag = goToAdd.tag;
            go.layer = goToAdd.layer;

            AddComponents(goToAdd.components, go);
        }

        // Removals
        foreach (GameObjectSnapshot goToRemove in levelModification.removedGameObjects)
        {
            GameObject obj = FindByGuid(goToRemove.guid);
            if (obj == null) continue;

#if UNITY_EDITOR
            DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }

        // Modifications
        foreach (GameObjectModification goToModify in levelModification.gameObjectModifications)
        {
            GameObject go = FindByGuid(goToModify.guid);
            if (go == null) continue;

            if (goToModify.oldParentGuid != goToModify.newParentGuid) go.transform.parent = FindByGuid(goToModify.newParentGuid).transform;
            if (goToModify.oldIsStatic != goToModify.newIsStatic) go.isStatic = goToModify.newIsStatic;
            if (goToModify.oldName != goToModify.newName) go.name = goToModify.newName;
            if (goToModify.oldActive != goToModify.newActive) go.SetActive(goToModify.newActive);
            if (goToModify.oldTag != goToModify.newTag) go.tag = goToModify.newTag;
            if (goToModify.oldLayer != goToModify.newLayer) go.layer = goToModify.newLayer;

            AddComponents(goToModify.addedComponents, go);
            RemoveComponents(goToModify.removedComponents, go);
            ModifyComponents(goToModify.componentModifications, go);
        }
    }

    private void Revert()
    {
        if (levelModification == null) return;

        // Additions
        foreach (GameObjectSnapshot goToAdd in levelModification.addedGameObjects)
        {
            GameObject obj = FindByGuid(goToAdd.guid);
            if (obj == null) continue;

#if UNITY_EDITOR
            DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }

        // Removals
        foreach (GameObjectSnapshot goToRemove in levelModification.removedGameObjects)
        {
            GameObject parent = FindByGuid(goToRemove.parentGuid);
            if (parent == null) continue;

            GameObject go = new GameObject(goToRemove.name);
            go.transform.SetParent(parent.transform);

            go.isStatic = goToRemove.isStatic;
            go.SetActive(goToRemove.active);
            go.tag = goToRemove.tag;
            go.layer = goToRemove.layer;

            AddComponents(goToRemove.components, go);
        }

        // Modifications
        foreach (GameObjectModification goToModify in levelModification.gameObjectModifications)
        {
            GameObject go = FindByGuid(goToModify.guid);
            if (go == null) continue;

            if (goToModify.oldParentGuid != goToModify.newParentGuid) go.transform.parent = FindByGuid(goToModify.oldParentGuid).transform;
            if (goToModify.oldIsStatic != goToModify.newIsStatic) go.isStatic = goToModify.oldIsStatic;
            if (goToModify.oldName != goToModify.newName) go.name = goToModify.oldName;
            if (goToModify.oldActive != goToModify.newActive) go.SetActive(goToModify.oldActive);
            if (goToModify.oldTag != goToModify.newTag) go.tag = goToModify.oldTag;
            if (goToModify.oldLayer != goToModify.newLayer) go.layer = goToModify.oldLayer;

            RemoveComponents(goToModify.addedComponents, go);
            AddComponents(goToModify.removedComponents, go);
            ModifyComponents(goToModify.componentModifications, go, true);
        }
    }

    private void AddComponents(List<ComponentSnapshot> componentsToAdd, GameObject go)
    {
        foreach (ComponentSnapshot componentToAdd in componentsToAdd)
        {
            Type compType = Type.GetType(componentToAdd.type);
            Component comp;
            if (compType == typeof(Transform)) comp = go.GetComponent<Transform>();
            else comp = go.AddComponent(compType);

            SetComponentEnabled(comp, componentToAdd.enabled);

            foreach (FieldSnapshot fieldToAdd in componentToAdd.fields) SetField(comp, fieldToAdd);
        }
    }
    private void RemoveComponents(List<ComponentSnapshot> componentsToRemove, GameObject go)
    {
        IEnumerable<IGrouping<string, ComponentSnapshot>> grouped = componentsToRemove.GroupBy(c => c.type);

        foreach (IGrouping<string, ComponentSnapshot> group in grouped)
        {
            List<Component> components = go.GetComponents<Component>().Where(c => c != null && c.GetType().AssemblyQualifiedName == group.Key).ToList();

            // Remove highest index first
            foreach (ComponentSnapshot compToRemove in group.OrderByDescending(c => c.index))
            {
                if (compToRemove.index >= components.Count) continue;
                Component component = components[compToRemove.index];

#if UNITY_EDITOR
                DestroyImmediate(component);
#else
                Destroy(component);
#endif
            }
        }
    }
    private void ModifyComponents(List<ComponentModification> componentModifications, GameObject go, bool revert = false)
    {
        foreach (ComponentModification modification in componentModifications)
        {
            Component component = FindComponent(go, modification.type, modification.index);
            foreach (FieldModification fieldModification in modification.fieldModifications)
            {
                FieldSnapshot field = revert ? fieldModification.before : fieldModification.after;
                SetField(component, field);
            }
        }
    }

    #region Helpers
    public static GameObject FindByGuid(string guid) => FindObjectsByType<SnapshotID>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.guid == guid)?.gameObject;
    private Component FindComponent(GameObject go, string type, int index)
    {
        List<Component> comps = go.GetComponents<Component>().Where(c => c.GetType().AssemblyQualifiedName == type).ToList();
        return index < comps.Count ? comps[index] : null;
    }
    public static void SetComponentEnabled(Component component, bool enabled)
    {
        if (component is Behaviour behaviour) behaviour.enabled = enabled;
        if (component is Renderer renderer) renderer.enabled = enabled;
        if (component is Collider collider) collider.enabled = enabled;
    }
    #endregion
    #endregion

    #region Field Setting
    private void SetField(Component target, FieldSnapshot field)
    {
        if (target == null) return;

        // Use authored apply logic for inaccessible fields
        if (ComponentApplierRegistry.TryApply(target, field)) return;

        // Fallback: try applying the change using reflections. (Should work for any component that isn't built-in)
        ApplyPath(target, target.GetType(), field.path.Split('.'), 0, () => field);
    }

    private object ApplyPath(object current, Type currentType, string[] path, int index, Func<FieldSnapshot> newValueProvider)
    {
        // If current is null we only create container/value instances.
        // Creating container instances (arrays/lists) is necessary so we can assign into indices.
        if (current == null)
        {
            if (currentType != null)
            {
                try
                {
                    if (currentType.IsArray) current = Array.CreateInstance(currentType.GetElementType(), 0);
                    else if (typeof(IList).IsAssignableFrom(currentType))
                    {
                        if (currentType.IsInterface || currentType.IsAbstract)
                        {
                            if (currentType.IsGenericType)
                            {
                                Type elemType = currentType.GetGenericArguments()[0];
                                Type listType = typeof(List<>).MakeGenericType(elemType);
                                current = Activator.CreateInstance(listType);
                            }
                            else current = new ArrayList();
                        }
                        else current = Activator.CreateInstance(currentType);
                    }
                    // Value types (structs) need an instance so we can set their fields
                    else if (currentType.IsValueType) current = Activator.CreateInstance(currentType);
                    else current = null; // Leaf values will be returned and assigned directly by the caller.
                }
                catch { current = null; } // Fallback: cannot instantiate container/value; leave as null so leaf returns final value
            }
            else return null;
        }

        // Leaf node
        if (index >= path.Length)
        {
            FieldSnapshot field = newValueProvider();

            // Resize array 
            if (field.type == SerializedValueType.ArraySize)
            {
                int newSize = field.GetAs<int>();
                if (current is Array arr) return ResizeArray(arr, current.GetType(), newSize);
                return current;
            }

            if (field.type == SerializedValueType.ObjectReference) return field.GetAsObject();
            return field.GetAsType(currentType);
        }
        if (index < 0 || index >= path.Length) return current;

        string token = path[index];
        // Array structure tokens (pass through)
        if (token == "Array" || token == "data" || token == "size") return ApplyPath(current, currentType, path, index + 1, newValueProvider);

        // Arrays / Lists
        if (token.Contains("["))
        {
            // Parse index
            int a = token.IndexOf('[');
            int b = token.IndexOf(']');
            int i = int.Parse(token.Substring(a + 1, b - a - 1));

            // Arrays
            if (current is Array array)
            {
                if (i < 0) return current;

                // Ensure array can hold index by resizing if necessary
                if (i >= array.Length) array = (Array)ResizeArray(array, array.GetType(), i + 1);

                object element = array.GetValue(i);

                // Determine element type even if element is null
                Type elementType = element?.GetType() ?? (array.GetType().HasElementType ? array.GetType().GetElementType() : typeof(object));

                object updatedElement = ApplyPath(element, elementType, path, index + 1, newValueProvider);

                array.SetValue(updatedElement, i);
                return array;
            }

            // Lists
            if (current is IList list)
            {
                if (i < 0) return current;

                Type listType = list.GetType();

                // If index out of range, grow list with default values to accommodate
                if (i >= list.Count)
                {
                    if (listType.IsGenericType)
                    {
                        Type elemType = listType.GetGenericArguments()[0];
                        for (int j = list.Count; j <= i; j++)
                        {
                            object defaultValue = elemType.IsValueType ? Activator.CreateInstance(elemType) : null;
                            list.Add(defaultValue);
                        }
                    }
                    else
                    {
                        for (int j = list.Count; j <= i; j++) list.Add(null);
                    }
                }

                object element = list[i];

                // Determine element type for generic lists, otherwise fallback to object
                Type elementType = element?.GetType();
                if (elementType == null)
                {
                    if (listType.IsGenericType)
                    {
                        Type[] args = listType.GetGenericArguments();
                        if (args != null && args.Length > 0) elementType = args[0];
                    }
                    if (elementType == null) elementType = typeof(object);
                }

                object updatedElement = ApplyPath(
                    element,
                    elementType,
                    path,
                    index + 1,
                    newValueProvider
                );

                list[i] = updatedElement;
                return list;
            }

            Debug.LogWarning($"Index access on non-collection: {token}");
            return current;
        }

        // Fields / Properties
        MemberInfo member = FindMember(currentType, token);
        if (member == null)
        {
            Debug.LogWarning($"Missing member {token} on {currentType}");
            return current;
        }
        object next = member switch
        {
            FieldInfo field => field.GetValue(current),
            PropertyInfo property => property.GetValue(current),
            _ => null
        };
        Type nextType = next != null ? next.GetType() : member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => null
        };

        object updatedNext = ApplyPath(next, nextType, path, index + 1, newValueProvider);

        // Set member value
        if (member is FieldInfo f)
        {
            if (updatedNext == null)
            {
                // Allow null assignment for reference types
                if (!f.FieldType.IsValueType) f.SetValue(current, null);
                else Debug.LogWarning($"Attempting to set null to value type field {f.Name} on {currentType}");
            }
            else
            {
                if (!f.FieldType.IsAssignableFrom(updatedNext.GetType())) Debug.LogWarning($"Type mismatch: {f.FieldType} <- {updatedNext?.GetType()} on {currentType}.{f.Name}");
                else f.SetValue(current, updatedNext);
            }
        }
        else if (member is PropertyInfo p)
        {
            if (updatedNext == null)
            {
                if (!p.PropertyType.IsValueType && p.CanWrite) p.SetValue(current, null);
                else Debug.LogWarning($"Attempting to set null to value type or read-only property {p.Name} on {currentType}");
            }
            else
            {
                if (!p.PropertyType.IsAssignableFrom(updatedNext.GetType())) Debug.LogWarning($"Type mismatch: {p.PropertyType} <- {updatedNext?.GetType()}");
                else if (p.CanWrite) p.SetValue(current, updatedNext);
            }
        }
        return current;
    }
    
    private static MemberInfo FindMember(Type type, string name)
    {
        while (type != null)
        {
            FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f;
            PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return p;
            type = type.BaseType;
        }
        return null;
    }
    
    private static object ResizeArray(Array currentArr, Type arrayType, int newSize)
    {
        if (currentArr == null) return Array.CreateInstance(arrayType.GetElementType(), newSize);
        Array resized = Array.CreateInstance(arrayType.GetElementType(), newSize);
        Array.Copy(currentArr, resized, Mathf.Min(currentArr.Length, newSize));
        return resized;
    }
    #endregion
}