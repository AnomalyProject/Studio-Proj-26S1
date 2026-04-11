using System.Collections.Generic;
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
/// </summary>
[ExecuteAlways]
public class ModificationApplier : MonoBehaviour
{
    public LevelModification levelModification;
    private bool applied = false;

    [Tooltip("Called after modification is applied.")]
    public UnityEvent onEnable;
    [Tooltip("Called before modification is reverted.")]
    public UnityEvent onDisable;

    private void Awake() => applied = gameObject.activeInHierarchy;

    private void OnEnable()
    {
        if (levelModification != null) Apply();
        onEnable?.Invoke();
    }

    private void OnDisable()
    {
        onDisable?.Invoke();
        if (levelModification != null) Revert();
    }

    private void Apply()
    {
        if (applied) return;
        applied = true;

        // ADDITIONS
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

        // REMOVALS
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

        // MODIFICATIONS
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
        if (!applied) return;
        applied = false;

        // ADDITIONS
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

        // REMOVALS
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

        // MODIFICATIONS
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

    private void AddComponents(List<ComponentSnapshot> compsToAdd, GameObject go)
    {
        foreach (ComponentSnapshot compToAdd in compsToAdd)
        {
            Type compType = Type.GetType(compToAdd.type);
            Component comp;
            if (compType == typeof(Transform)) comp = go.GetComponent<Transform>();
            else comp = go.AddComponent(compType);

            SetComponentEnabled(comp, compToAdd.enabled);

            foreach (FieldSnapshot fieldToAdd in compToAdd.fields) SetField(comp, fieldToAdd);
        }
    }
    private void RemoveComponents(List<ComponentSnapshot> compsToRemove, GameObject go)
    {
        IEnumerable<IGrouping<string, ComponentSnapshot>> grouped = compsToRemove.GroupBy(c => c.type);

        foreach (IGrouping<string, ComponentSnapshot> group in grouped)
        {
            List<Component> comps = go.GetComponents<Component>().Where(c => c != null && c.GetType().AssemblyQualifiedName == group.Key).ToList();

            // Remove highest index first
            foreach (ComponentSnapshot compToRemove in group.OrderByDescending(c => c.index))
            {
                if (compToRemove.index >= comps.Count) continue;
                Component comp = comps[compToRemove.index];

#if UNITY_EDITOR
                DestroyImmediate(comp);
#else
                Destroy(comp);
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
    private GameObject FindByGuid(string guid) => FindObjectsByType<SnapshotID>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.guid == guid)?.gameObject;
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
    private void SetField(Component target, FieldSnapshot field)
    {
        // Use authored apply logic for inaccessible fields
        if (ComponentApplierRegistry.TryApply(target, field)) return;

        // Fallback: try applying the change using reflections. (Should work for any component that isn't built-in)
        string[] elements = field.path.Split('.');
        object current = target;
        Type currentType = target.GetType();
        for (int i = 0; i < elements.Length; i++)
        {
            string element = elements[i];

            // Handle arrays/lists: data[x]
            if (element == "Array")
            {
                i++; // skip "data[x]"
                int index = int.Parse(elements[i].Replace("data[", "").Replace("]", ""));
                if (current is System.Collections.IList list)
                {
                    if (index >= list.Count) return;
                    current = list[index];
                    currentType = current.GetType();
                    continue;
                }
                return;
            }

            // Try getting field, then property if it fails
            FieldInfo fieldInfo = currentType.GetField(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo propertyInfo = null;
            if (fieldInfo == null) propertyInfo = currentType.GetProperty(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // Unity internal or unsupported path
            if (fieldInfo == null && propertyInfo == null)
            {
                Debug.LogWarning($"Skipping unsupported path: {field.path}");
                return;
            }

            // Set the value if you find the last element
            if (i == elements.Length - 1)
            {
                object value = field.GetAsType(fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType);
                if (fieldInfo != null) fieldInfo.SetValue(current, value);
                else if (propertyInfo != null && propertyInfo.CanWrite) propertyInfo.SetValue(current, value);
            }
            else // Recurse
            {
                if (fieldInfo != null)
                {
                    current = fieldInfo.GetValue(current);
                    currentType = fieldInfo.FieldType;
                }
                else
                {
                    current = propertyInfo.GetValue(current);
                    currentType = propertyInfo.PropertyType;
                }

                if (current == null) return;
            }
        }
    }
    #endregion
}