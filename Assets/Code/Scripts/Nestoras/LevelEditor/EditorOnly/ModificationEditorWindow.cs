#if UNITY_EDITOR
using System.Collections.Generic;
using static SnapshotUtility;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Level editor window allowing the creation of <see cref="LevelModification"/>s using <see cref="SnapshotUtility"/>.
/// Draws a diagram of the file that's about to be created.
/// Displays warnings when modifications may not be reproduced correctly in standalone builds (Captured fields of native components without custom appliers).
/// </summary>
public class ModificationEditorWindow : EditorWindow
{
    private GameObject lastRoot;
    private GameObject root; // Root of the hierarchy being tracked
    private Transform modificationsRoot; // Parent of modification objects.

    private string modificationName;
    private string lastSaveLocation;

    private static List<GameObjectSnapshot> baseline;
    private static DiffResult lastDiff;

    private Vector2 scroll;
    private static GUIStyle rightAlignedLabel;
    private static GUIStyle overflowLabel;

    private static bool baselineHasNativeFieldWithoutApplier;
    public static bool capturedNativeFieldWithoutApplier;

    // Foldout state caches
    private Dictionary<string, bool> gameObjectFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, bool> componentFoldouts = new Dictionary<string, bool>();

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        baseline = null;
        baselineHasNativeFieldWithoutApplier = false;
        capturedNativeFieldWithoutApplier = false;
    }

    [MenuItem("Window/Modification Editor")]
    public static void Open() => GetWindow<ModificationEditorWindow>("Modification Editor");

    private void OnGUI()
    {
        // Clear snapshots when changing objects
        if (root != null && lastRoot != root)
        {
            baseline = null;
            lastDiff = null;
            baselineHasNativeFieldWithoutApplier = false;
            capturedNativeFieldWithoutApplier = false;
        }
        lastRoot = root;

        // Scene References
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Level Root", GUILayout.Width(65));
        root = (GameObject)EditorGUILayout.ObjectField(root, typeof(GameObject), true, GUILayout.ExpandWidth(false));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Modifications Root", GUILayout.Width(110));
        modificationsRoot = (Transform)EditorGUILayout.ObjectField(modificationsRoot, typeof(Transform), true, GUILayout.ExpandWidth(false));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        // Capture initial state of the hierarchy
        GUI.enabled = root != null;
        bool hasBaseline = baseline != null && baseline.Count > 0;
        Color defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = hasBaseline ? Color.lawnGreen : Color.softRed;
        if (GUILayout.Button("Capture Baseline"))
        {
            baseline = Capture(root);
            lastDiff = null;
            baselineHasNativeFieldWithoutApplier = capturedNativeFieldWithoutApplier;
            capturedNativeFieldWithoutApplier = false;
        }
        GUI.backgroundColor = defaultColor;

        // Compare current hierarchy state against baseline
        GUI.enabled &= hasBaseline;
        if (GUILayout.Button("Capture Modifications"))
        {
            capturedNativeFieldWithoutApplier = false;
            lastDiff = Diff(baseline, Capture(root));
        }
        EditorGUILayout.EndHorizontal();

        // Draw Diagram
        DrawDiff();

        GUI.enabled &= modificationsRoot != null;
        EditorGUILayout.BeginHorizontal();
        modificationName = EditorGUILayout.TextField(string.IsNullOrWhiteSpace(modificationName) ? (modificationsRoot == null ? string.Empty : $"Modification {modificationsRoot.childCount + 1}") : modificationName, GUILayout.ExpandWidth(true));
        GUI.enabled &= lastDiff != null;
        if (GUILayout.Button("Save Asset", GUILayout.Width(80)))
        {
            // Save file
            LevelModification modification = BuildLevelModification(lastDiff);
            string path = EditorUtility.SaveFilePanelInProject("Save Modification", modificationName, "asset", "Choose location", lastSaveLocation ?? "Assets/Scenes");
            if (!string.IsNullOrEmpty(path))
            {
                lastSaveLocation = path;
                AssetDatabase.CreateAsset(modification, lastSaveLocation);
                AssetDatabase.SaveAssets();

                // Create new modification applier
                GameObject modificationObject = Instantiate(new GameObject(), modificationsRoot);
                modificationObject.name = modificationName;
                ModificationApplier modificationApplier = modificationObject.AddComponent<ModificationApplier>();
                EditorApplication.delayCall += () => modificationApplier.levelModification = modification;
            }
        }

        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;
    }

    private void DrawDiff()
    {
        Color defaultColor = GUI.contentColor;
        bool GUIenabled = GUI.enabled;
        GUI.enabled = true;

        EditorGUILayout.Space();
        GUIContent labelContent = lastDiff != null && baselineHasNativeFieldWithoutApplier != capturedNativeFieldWithoutApplier ? new GUIContent("Modifications", EditorGUIUtility.IconContent("console.warnicon").image, "Captured fields of native components without custom appliers: setting these values via reflection may fail.") : new GUIContent("Modifications");
        EditorGUILayout.LabelField(labelContent, EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, false, false, GUILayout.ExpandHeight(true));
        if (lastDiff == null) EditorGUILayout.HelpBox("No modifications captured yet.\nCapture a baseline and modifications to view changes.", MessageType.Info);
        else
        {
            // Added
            if (lastDiff.added.Count > 0)
            {
                GUI.contentColor = Color.green;
                EditorGUILayout.LabelField("+ Added GameObjects", EditorStyles.boldLabel);
                GUI.contentColor = defaultColor;
                foreach (GameObjectSnapshot added in lastDiff.added)
                {
                    string goKey = $"added:{added.guid}";
                    gameObjectFoldouts.TryGetValue(goKey, out bool open);
                    open = EditorGUILayout.Foldout(open, $"+ {added.name}", true);
                    gameObjectFoldouts[goKey] = open;
                    if (open)
                    {
                        EditorGUI.indentLevel++;
                        DrawGameObjectSnapshotDetails(added, showAllFields: true);
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                }
            }

            // Removed
            if (lastDiff.removed.Count > 0)
            {
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("- Removed GameObjects", EditorStyles.boldLabel);
                GUI.contentColor = defaultColor;
                foreach (GameObjectSnapshot removed in lastDiff.removed)
                {
                    string goKey = $"removed:{removed.guid}";
                    gameObjectFoldouts.TryGetValue(goKey, out bool open);
                    open = EditorGUILayout.Foldout(open, $"- {removed.name}", true);
                    gameObjectFoldouts[goKey] = open;
                    if (open)
                    {
                        EditorGUI.indentLevel++;
                        DrawGameObjectSnapshotDetails(removed, showAllFields: true);
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                }
            }

            // Modified
            if (lastDiff.modified.Count > 0)
            {
                GUI.contentColor = Color.yellowNice;
                EditorGUILayout.LabelField("* Modified GameObjects", EditorStyles.boldLabel);
                GUI.contentColor = defaultColor;
                foreach ((GameObjectSnapshot before, GameObjectSnapshot after) in lastDiff.modified)
                {
                    string guid = before.guid;
                    string goKey = $"changed:{guid}";
                    gameObjectFoldouts.TryGetValue(goKey, out bool goOpen);
                    goOpen = EditorGUILayout.Foldout(goOpen, $"* {after.name}", true);
                    gameObjectFoldouts[goKey] = goOpen;
                    if (goOpen)
                    {
                        EditorGUI.indentLevel++;

                        // Show GameObject-level modifications
                        DrawGameObjectPropertyIfChanged("name", before.name, after.name);
                        DrawGameObjectPropertyIfChanged("isStatic", before.isStatic, after.isStatic);
                        DrawGameObjectPropertyIfChanged("active", before.active, after.active);
                        DrawGameObjectPropertyIfChanged("tag", before.tag, after.tag);
                        DrawGameObjectPropertyIfChanged("layer", before.layer, after.layer);
                        // Check for difference in parent GUID, not just name.
                        if (before.parentGuid != after.parentGuid) DrawAlignedModification("parent", GetParentNameFromGuid(before.parentGuid), GetParentNameFromGuid(after.parentGuid), true);

                        // Compare components
                        Dictionary<string, ComponentSnapshot> mapA = before.components.ToDictionary(component => component.type + "#" + component.index);
                        Dictionary<string, ComponentSnapshot> mapB = after.components.ToDictionary(component => component.type + "#" + component.index);

                        // Added components
                        foreach (KeyValuePair<string, ComponentSnapshot> component in mapB)
                        {
                            if (!mapA.ContainsKey(component.Key))
                            {
                                // New component
                                string componentKey = $"{guid}:component:{component.Key}";
                                componentFoldouts.TryGetValue(componentKey, out bool componentOpen);
                                componentOpen = EditorGUILayout.Foldout(componentOpen, $"+ {System.Type.GetType(component.Value.type).ToString().Split('.')[^1]} #{component.Value.index + 1}", true);
                                componentFoldouts[componentKey] = componentOpen;
                                if (componentOpen)
                                {
                                    EditorGUI.indentLevel++;
                                    DrawComponentSnapshotDetails(component.Value, showAllFields: true);
                                    EditorGUI.indentLevel--;
                                }
                            }
                        }

                        // Removed components
                        foreach (KeyValuePair<string, ComponentSnapshot> component in mapA)
                        {
                            if (!mapB.ContainsKey(component.Key))
                            {
                                string componentKey = $"{guid}:component:{component.Key}";
                                componentFoldouts.TryGetValue(componentKey, out bool componentOpen);
                                componentOpen = EditorGUILayout.Foldout(componentOpen, $"- {System.Type.GetType(component.Value.type).ToString().Split('.')[^1]} #{component.Value.index + 1}", true);
                                componentFoldouts[componentKey] = componentOpen;
                                if (componentOpen)
                                {
                                    EditorGUI.indentLevel++;
                                    DrawComponentSnapshotDetails(component.Value, showAllFields: true);
                                    EditorGUI.indentLevel--;
                                }
                            }
                        }

                        // Modified components and their fields
                        foreach (KeyValuePair<string, ComponentSnapshot> component in mapA)
                        {
                            if (!mapB.TryGetValue(component.Key, out ComponentSnapshot snapshotB)) continue;
                            ComponentSnapshot snapshotA = component.Value;

                            // Compare field lists
                            Dictionary<string, FieldSnapshot> fieldMapA = snapshotA.fields.ToDictionary(field => field.path);
                            Dictionary<string, FieldSnapshot> fieldMapB = snapshotB.fields.ToDictionary(field => field.path);

                            // Check whether any field has been modified
                            List<string> modifiedPaths = new List<string>();
                            foreach (string path in fieldMapA.Keys)
                            {
                                if (!fieldMapB.ContainsKey(path)) modifiedPaths.Add(path);
                                else if (!fieldMapA[path].Equals(fieldMapB[path])) modifiedPaths.Add(path);
                            }
                            foreach (string path in fieldMapB.Keys) if (!fieldMapA.ContainsKey(path)) modifiedPaths.Add(path);

                            // Skip if no modifications are found
                            if (modifiedPaths.Count == 0 && snapshotA.enabled == snapshotB.enabled) continue;

                            string componentKey = $"{guid}:component:{component.Key}";
                            componentFoldouts.TryGetValue(componentKey, out bool componentOpen);
                            componentOpen = EditorGUILayout.Foldout(componentOpen, $"* {System.Type.GetType(component.Value.type).ToString().Split('.')[^1]} #{component.Value.index + 1}", true);
                            componentFoldouts[componentKey] = componentOpen;
                            if (componentOpen)
                            {
                                EditorGUI.indentLevel++;

                                // Explicitly check enabled state since it's not part of the field list
                                if (snapshotA.enabled != snapshotB.enabled) DrawAlignedModification("Enabled", snapshotA.enabled.ToString(), snapshotB.enabled.ToString());

                                // Check each field
                                foreach (string path in modifiedPaths)
                                {
                                    fieldMapA.TryGetValue(path, out FieldSnapshot fieldB);
                                    string beforeValue = fieldB != null ? FieldSnapshotToDisplayString(fieldB) : "<missing>";
                                    fieldMapB.TryGetValue(path, out FieldSnapshot fieldA);
                                    string afterValue = fieldA != null ? FieldSnapshotToDisplayString(fieldA) : "<missing>";

                                    DrawAlignedModification(path, beforeValue, afterValue, ComponentApplierRegistry.IsFieldSupported(System.Type.GetType(snapshotA.type), path), !System.Type.GetType(snapshotA.type).IsSubclassOf(typeof(MonoBehaviour)));
                                }

                                EditorGUI.indentLevel--;
                            }
                        }

                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
        GUI.enabled = GUIenabled;
    }

    #region Helpers
    // Drawers
    private void DrawGameObjectSnapshotDetails(GameObjectSnapshot snapshot, bool showAllFields)
    {
        DrawAlignedProperty("Name", snapshot.name, true);
        DrawAlignedProperty("Parent", GetParentNameFromGuid(snapshot.parentGuid), true);
        DrawAlignedProperty("Static", snapshot.isStatic.ToString(), true);
        DrawAlignedProperty("Active", snapshot.active.ToString(), true);
        DrawAlignedProperty("Tag", snapshot.tag, true);
        DrawAlignedProperty("Layer", snapshot.layer.ToString(), true);

        if (snapshot.components != null && snapshot.components.Count > 0)
        {
            DrawAlignedProperty("Components", $"{snapshot.components.Count}");
            EditorGUI.indentLevel++;
            foreach (ComponentSnapshot component in snapshot.components)
            {
                string componentKey = $"{snapshot.guid}:component:{component.type}#{component.index}";
                componentFoldouts.TryGetValue(componentKey, out bool open);
                open = EditorGUILayout.Foldout(open, $"{System.Type.GetType(component.type).ToString().Split('.')[^1]} #{component.index + 1}", true);
                componentFoldouts[componentKey] = open;
                if (open)
                {
                    EditorGUI.indentLevel++;
                    DrawComponentSnapshotDetails(component, showAllFields);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
        }
    }
    private void DrawComponentSnapshotDetails(ComponentSnapshot componentSnapshot, bool showAllFields)
    {
        DrawAlignedProperty("Enabled", componentSnapshot.enabled.ToString(), true);
        if (componentSnapshot.fields != null && componentSnapshot.fields.Count > 0)
        {
            DrawAlignedProperty("Fields", $"{componentSnapshot.fields.Count}");
            EditorGUI.indentLevel++;
            foreach (FieldSnapshot f in componentSnapshot.fields) if (showAllFields) DrawAlignedProperty(f.path, FieldSnapshotToDisplayString(f), ComponentApplierRegistry.IsFieldSupported(System.Type.GetType(componentSnapshot.type), f.path), !System.Type.GetType(componentSnapshot.type).IsSubclassOf(typeof(MonoBehaviour)));
            EditorGUI.indentLevel--;
        }
    }
    private void DrawGameObjectPropertyIfChanged<T>(string name, T before, T after)
    {
        if (!Equals(before, after)) DrawAlignedModification(name, before?.ToString() ?? "<null>", after?.ToString() ?? "<null>", true);
    }
    private void DrawAlignedModification(string name, string before, string after, bool explicitlySupportedByApplier = false, bool fromMonoBehaviour = false) => DrawAlignedProperty(name, $"{before} -> {after}", explicitlySupportedByApplier, fromMonoBehaviour);
    private void DrawAlignedProperty(string name, string value, bool explicitlySupportedByApplier = false, bool reflectionMayFail = false)
    {
        if (rightAlignedLabel == null) rightAlignedLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, clipping = TextClipping.Overflow };
        if (overflowLabel == null) overflowLabel = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Overflow };

        bool drawWarning = false;
        string tooltip = null;

        Color guiColor = GUI.contentColor;
        if (value.Contains("<unsupported>") || value.Contains("<error>"))
        {
            tooltip = "Field type cannot be serialized / captured.";
            GUI.contentColor = Color.red;
        }
        else if (explicitlySupportedByApplier)
        {
            tooltip = "Field is supported by a custom applier: it will be set via explicit API calls in standalone builds.";
            GUI.contentColor = Color.lightGreen;
        }
        else if (reflectionMayFail)
        {
            drawWarning = true;
            tooltip = "Native component field without custom applier: setting this value via reflection may fail.";
            GUI.contentColor = Color.yellowNice;
        }

        EditorGUILayout.BeginHorizontal();
        Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(name), overflowLabel);
        EditorGUI.LabelField(labelRect, new GUIContent(name, tooltip), overflowLabel);
        if (drawWarning)
        {
            float iconSize = EditorGUIUtility.singleLineHeight - 5;
            Rect iconRect = new Rect(labelRect.xMin + (EditorGUI.indentLevel - 1) * 15, labelRect.y + 2.5f, iconSize, iconSize);
            GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("console.warnicon").image);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(value, rightAlignedLabel);
        EditorGUILayout.EndHorizontal();
        GUI.contentColor = guiColor;
    }

    // Parsers
    private string FieldSnapshotToDisplayString(FieldSnapshot field)
    {
        if (field == null) return "<null>";
        try
        {
            switch (field.type)
            {
                case SerializedValueType.Boolean:
                    return field.GetAs<bool>().ToString();
                case SerializedValueType.Integer:
                case SerializedValueType.ArraySize:
                case SerializedValueType.Enum:
                case SerializedValueType.LayerMask:
                    return field.GetAs<int>().ToString();
                case SerializedValueType.Float:
                    return field.GetAs<float>().ToString();
                case SerializedValueType.String:
                    return field.GetAs<string>() ?? "<empty>";
                case SerializedValueType.Vector2:
                    Vector2 vector2 = field.GetAs<Vector2>();
                    return $"({FormatFloat(vector2.x)}, {FormatFloat(vector2.y)})";
                case SerializedValueType.Vector3:
                    Vector3 vector3 = field.GetAs<Vector3>();
                    return $"({FormatFloat(vector3.x)}, {FormatFloat(vector3.y)}, {FormatFloat(vector3.z)})";
                case SerializedValueType.Vector4:
                    Vector4 vector4 = field.GetAs<Vector4>();
                    return $"({FormatFloat(vector4.x)}, {FormatFloat(vector4.y)}, {FormatFloat(vector4.z)}, {FormatFloat(vector4.w)})";
                case SerializedValueType.Quaternion:
                    Vector3 euler = field.GetAs<Quaternion>().eulerAngles;
                    return $"({FormatFloat(euler.x)}, {FormatFloat(euler.y)}, {FormatFloat(euler.z)})";
                case SerializedValueType.Color:
                    Color color = field.GetAs<Color>();
                    return $"({FormatFloat(color.r)}, {FormatFloat(color.g)}, {FormatFloat(color.b)}, {FormatFloat(color.a)})";
                case SerializedValueType.ObjectReference:
                    string guid = field.GetAs<string>();
                    if (string.IsNullOrEmpty(guid)) return "<null>";
                    Object obj = null;
                    try { obj = ComponentApplierRegistry.objectGuidRegistry.Get(guid); } catch { obj = null; }
                    return obj != null ? $"{obj.name} ({obj.GetType().Name})" : guid;
                case SerializedValueType.AnimationCurve:
                    AnimationCurve ac = field.GetAs<AnimationCurve>();
                    return ac != null ? "<animation curve>" : "<null>";
                case SerializedValueType.Gradient:
                    Gradient gradient = field.GetAs<Gradient>();
                    return gradient != null ? "<gradient>" : "<null>";
                case SerializedValueType.Rect:
                    Rect rect = field.GetAs<Rect>();
                    return $"(x:{FormatFloat(rect.x)}, y:{FormatFloat(rect.y)}, w:{FormatFloat(rect.width)}, h:{FormatFloat(rect.height)})";
                case SerializedValueType.Bounds:
                    Bounds bounds = field.GetAs<Bounds>();
                    return $"(center:({FormatFloat(bounds.center.x)}, {FormatFloat(bounds.center.y)}, {FormatFloat(bounds.center.z)}), size:({FormatFloat(bounds.size.x)}, {FormatFloat(bounds.size.y)}, {FormatFloat(bounds.size.z)}))";
                case SerializedValueType.Vector2Int:
                    return field.GetAs<Vector2Int>().ToString();
                case SerializedValueType.Vector3Int:
                    return field.GetAs<Vector3Int>().ToString();
                case SerializedValueType.RectInt:
                    RectInt rectInt = field.GetAs<RectInt>();
                    return $"(x:{rectInt.x}, y:{rectInt.y}, w:{rectInt.width}, h:{rectInt.height})";
                case SerializedValueType.BoundsInt:
                    BoundsInt boundsInt = field.GetAs<BoundsInt>();
                    return $"(position:({boundsInt.position.x}, {boundsInt.position.y}, {boundsInt.position.z}), size:({boundsInt.size.x}, {boundsInt.size.y}, {boundsInt.size.z}))";
                case SerializedValueType.Character:
                    return field.GetAs<char>().ToString();
                default:
                    return "<unsupported>";
            }
        }
        catch { return "<error>"; }
    }
    private static string FormatFloat(float f) => f.ToString("0.####"); // Trim trailing zeros (max 4)
    private string GetObjectNameFromGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return "<none>";
        try
        {
            Object obj = ComponentApplierRegistry.objectGuidRegistry.Get(guid);
            if (obj != null) return obj.name;
            return guid;
        }
        catch { return guid; }
    }
    private string GetParentNameFromGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return "<none>";
        GameObject go = ModificationApplier.FindByGuid(guid);
        if (go != null) return go.name;
        return guid;
    }
    #endregion
}
#endif