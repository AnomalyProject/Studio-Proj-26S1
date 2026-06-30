#if UNITY_EDITOR
using System.Collections.Generic;
using static SnapshotUtility;
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Level editor window allowing the creation of <see cref="LevelModification"/>s using <see cref="SnapshotUtility"/>.
/// Draws a diagram of the file that's about to be created.
/// Displays warnings when modifications may not be reproduced correctly in standalone builds (Captured fields of native components without custom appliers).
/// </summary>
public class ModificationEditorWindow : EditorWindow
{
    private GameObject root; // Root of the hierarchy being tracked
    private GameObject lastRoot; // For detecting changes in the root object and clearing baseline/modifications
    private Transform modificationsRoot; // Parent of modification objects

    public static List<GameObjectSnapshot> baseline { get; private set; } // Original state of the hierarchy
    private static LevelModification lastModification; // Difference between the baseline and the latest captured state of the hierarchy

    private string modificationName; // For modification asset naming and corresponding applier gameobject naming
    private static string lastSaveLocation; // For remembering the last save location when saving multiple modifications

    // For warning
    public static bool diffContainsNativeFieldWithoutApplier;

    // Diagram UI
    private Vector2 scroll;
    private static GUIStyle rightAlignedLabel;
    private static GUIStyle overflowLabel;
    private bool darkerBackground;
    private static Color dark = new Color(0.1647059f, 0.1647059f, 0.1647059f);

    // Foldout state caches
    private Dictionary<string, bool> gameObjectFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, bool> componentFoldouts = new Dictionary<string, bool>();

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        baseline = null;
        diffContainsNativeFieldWithoutApplier = false;
    }

    private void ClearSnapshots(bool clearModificationName = true)
    {
        baseline = null;
        lastModification = null;
        diffContainsNativeFieldWithoutApplier = false;
        if (clearModificationName) modificationName = null;
    }

    [MenuItem("Window/Modification Editor")]
    public static void Open() => GetWindow<ModificationEditorWindow>("Modification Editor");

    private void OnGUI()
    {
        // Clear snapshots when changing objects
        if (lastRoot != root) ClearSnapshots();
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
            ClearSnapshots(false);
            baseline = Capture(root);
        }
        GUI.backgroundColor = defaultColor;

        // Compare current hierarchy state against baseline
        GUI.enabled &= hasBaseline;
        if (GUILayout.Button("Capture Modifications"))
        {
            diffContainsNativeFieldWithoutApplier = false;
            lastModification = Diff(baseline, Capture(root));
        }

        EditorGUILayout.EndHorizontal();

        // Draw Diagram
        bool GUIenabled = GUI.enabled;
        GUI.enabled = true;
        DrawDiff();

        EditorGUILayout.BeginHorizontal();

        // Info button
        GUIContent infoButtonContent = new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image, "Info");
        Rect infoRect = GUILayoutUtility.GetRect(infoButtonContent, EditorStyles.iconButton);
        infoRect.y += 2.5f; // Align with text field and other buttons
        if (GUI.Button(infoRect, infoButtonContent, EditorStyles.iconButton)) ModificationEditorInfoPopup.ShowWindow();

        // Clean ObjectGUIDRegistry button
        GUIContent cleanButtonContent = new GUIContent(ObjectGuidRegistryUtility.registryIcon, "Clean ObjectGUIDRegistry of unused entries");
        Rect cleanRect = GUILayoutUtility.GetRect(cleanButtonContent, EditorStyles.iconButton);
        cleanRect.y += 2.5f; // Align with text field and other buttons
        cleanRect.x -= 1;
        if (GUI.Button(cleanRect, cleanButtonContent, EditorStyles.iconButton))
        {
            ObjectGuidRegistryUtility.CleanRegistry();
            ClearSnapshots(); // Force user to recapture baseline to avoid referencing deleted entries and causing errors when applying modifications
        }
        float iconOffset = EditorGUIUtility.singleLineHeight - 10f;
        cleanRect.x += iconOffset;
        cleanRect.y += iconOffset;
        cleanRect.width /= 1.5f;
        cleanRect.height /= 1.5f;
        GUI.DrawTexture(cleanRect, EditorGUIUtility.IconContent("d_TreeEditor.Trash").image);

        // Name of the modification to be saved, defaulting to "Modification n" based on the number of children on the modifications root.
        GUI.enabled = GUIenabled && modificationsRoot != null;
        modificationName = EditorGUILayout.TextField(string.IsNullOrWhiteSpace(modificationName) ? (modificationsRoot == null ? string.Empty : $"Modification {modificationsRoot.childCount + 1}") : modificationName, GUILayout.ExpandWidth(true));

        // Save asset
        GUI.enabled &= lastModification != null;
        if (GUILayout.Button("Save Asset", GUILayout.Width(80)))
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Modification", modificationName, "asset", "Choose location", lastSaveLocation ?? "Assets/Anomalies");
            if (!string.IsNullOrEmpty(path))
            {
                lastSaveLocation = path;
                try // If saving the same modifications on the same path, AssetDatabase.CreateAsset will throw an error
                {
                    AssetDatabase.CreateAsset(lastModification, lastSaveLocation);
                    AssetDatabase.SaveAssets();

                    // Try to assign the new asset to any existing ModificationApplier with the same name, so that you can more easily update old modifications.
                    ModificationApplier modificationApplier = null;
                    foreach (ModificationApplier applier in modificationsRoot.GetComponentsInChildren<ModificationApplier>(true))
                    {
                        if (applier.gameObject.name == modificationName)
                        {
                            modificationApplier = applier;
                            applier.levelModification = lastModification;
                            break;
                        }
                    }
                    if (modificationApplier == null)
                    {
                        // Create new modification applier gameobject and wait a frame before assigning the asset to avoid immediate application.
                        GameObject modificationObject = new GameObject(modificationName);
                        modificationObject.transform.parent = modificationsRoot;
                        modificationObject.transform.localPosition = Vector3.zero;
                        modificationApplier = modificationObject.AddComponent<ModificationApplier>();
                        EditorApplication.delayCall += () => modificationApplier.levelModification = lastModification;

                        // Add to map
                        AnomalyMap map = modificationsRoot.GetComponentInParent<AnomalyMap>();
                        map.AddVariation(new AnomalyGroup(modificationObject, false));
                    }
                }
                catch { }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        GUI.enabled = true;
    }

    private void DrawDiff()
    {
        EditorGUILayout.Space();
        GUIContent labelContent = lastModification != null && diffContainsNativeFieldWithoutApplier ? new GUIContent("Modifications", EditorGUIUtility.IconContent("console.warnicon").image, "Captured fields of native components without custom appliers: setting these values via reflection may fail.") : new GUIContent("Modifications");
        EditorGUILayout.LabelField(labelContent, EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, false, false, GUILayout.ExpandHeight(true));
        if (lastModification == null) EditorGUILayout.HelpBox("No modifications captured yet.\nCapture a baseline and modifications to view changes.", MessageType.Info);
        else
        {
            Color defaultColor = GUI.contentColor;

            // Added
            if (lastModification.addedGameObjects.Count > 0)
            {
                GUI.contentColor = Color.green;
                EditorGUILayout.LabelField("+ Added GameObjects", EditorStyles.boldLabel);
                GUI.contentColor = defaultColor;
                foreach (GameObjectSnapshot added in lastModification.addedGameObjects)
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
            if (lastModification.removedGameObjects.Count > 0)
            {
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("- Removed GameObjects", EditorStyles.boldLabel);
                GUI.contentColor = defaultColor;
                foreach (GameObjectSnapshot removed in lastModification.removedGameObjects)
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
            if (lastModification.gameObjectModifications.Count > 0)
            {
                GUI.contentColor = Color.yellowNice;
                EditorGUILayout.LabelField("* Modified GameObjects", EditorStyles.boldLabel);
                GUI.contentColor = defaultColor;
                foreach (GameObjectModification goModification in lastModification.gameObjectModifications)
                {
                    string guid = goModification.guid;
                    string goKey = $"changed:{guid}";
                    gameObjectFoldouts.TryGetValue(goKey, out bool goOpen);
                    goOpen = EditorGUILayout.Foldout(goOpen, $"* {goModification.newName}", true);
                    gameObjectFoldouts[goKey] = goOpen;
                    if (goOpen)
                    {
                        darkerBackground = true;
                        EditorGUI.indentLevel++;

                        // Show GameObject-level modifications
                        DrawGameObjectPropertyIfChanged("name", goModification.oldName, goModification.newName);
                        DrawGameObjectPropertyIfChanged("isStatic", goModification.oldIsStatic, goModification.newIsStatic);
                        DrawGameObjectPropertyIfChanged("active", goModification.oldActive, goModification.newActive);
                        DrawGameObjectPropertyIfChanged("tag", goModification.oldTag, goModification.newTag);
                        DrawGameObjectPropertyIfChanged("layer", goModification.oldLayer, goModification.newLayer);
                        // Check for difference in parent GUID, not just name.
                        if (goModification.oldParentGuid != goModification.newParentGuid) DrawAlignedModification("parent", GetParentNameFromGuid(goModification.oldParentGuid), GetParentNameFromGuid(goModification.newParentGuid), true);

                        // Added components
                        foreach (ComponentSnapshot component in goModification.addedComponents)
                        {
                            // New component
                            string componentKey = $"{guid}:component:{component.type}{component.index}";
                            componentFoldouts.TryGetValue(componentKey, out bool componentOpen);
                            componentOpen = EditorGUILayout.Foldout(componentOpen, $"+ {System.Type.GetType(component.type).ToString().Split('.')[^1]} #{component.index + 1}", true);
                            componentFoldouts[componentKey] = componentOpen;
                            if (componentOpen)
                            {
                                EditorGUI.indentLevel++;
                                DrawComponentSnapshotDetails(component, showAllFields: true);
                                EditorGUI.indentLevel--;
                            }
                        }

                        // Removed components
                        foreach (ComponentSnapshot component in goModification.removedComponents)
                        {
                            string componentKey = $"{guid}:component:{component.type}{component.index}";
                            componentFoldouts.TryGetValue(componentKey, out bool componentOpen);
                            componentOpen = EditorGUILayout.Foldout(componentOpen, $"- {System.Type.GetType(component.type).ToString().Split('.')[^1]} #{component.index + 1}", true);
                            componentFoldouts[componentKey] = componentOpen;
                            if (componentOpen)
                            {
                                EditorGUI.indentLevel++;
                                DrawComponentSnapshotDetails(component, showAllFields: true);
                                EditorGUI.indentLevel--;
                            }
                        }

                        // Modified components and their fields
                        foreach (ComponentModification component in goModification.componentModifications)
                        {
                            string componentKey = $"{guid}:component:{component.type}{component.index}";
                            componentFoldouts.TryGetValue(componentKey, out bool componentOpen);
                            componentOpen = EditorGUILayout.Foldout(componentOpen, $"* {System.Type.GetType(component.type).ToString().Split('.')[^1]} #{component.index + 1}", true);
                            componentFoldouts[componentKey] = componentOpen;
                            if (componentOpen)
                            {
                                EditorGUI.indentLevel++;
                                // Explicitly check enabled state since it's not part of the field list
                                if (component.oldEnabled != component.newEnabled) DrawAlignedModification("Enabled", component.oldEnabled.ToString(), component.newEnabled.ToString(), true);
                                // Draw each field modification with optional warnings / green text to indicate support by custom appliers in standalone builds
                                foreach (FieldModification field in component.fieldModifications) DrawAlignedModification(field.before.path, FieldSnapshotToDisplayString(field.before), FieldSnapshotToDisplayString(field.after), ComponentApplierRegistry.IsFieldSupported(System.Type.GetType(component.type), field.before.path), !System.Type.GetType(component.type).IsSubclassOf(typeof(MonoBehaviour)));
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
    }

    #region Helpers
    // Drawers
    private void DrawGameObjectSnapshotDetails(GameObjectSnapshot snapshot, bool showAllFields)
    {
        darkerBackground = true;
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
        if (rightAlignedLabel == null) rightAlignedLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, clipping = TextClipping.Overflow, padding = new RectOffset(0, 30, 0, 0) };
        if (overflowLabel == null) overflowLabel = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Overflow };

        // Draw background
        Rect rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
        if (darkerBackground && Event.current.type == EventType.Repaint) EditorGUI.DrawRect(rowRect, dark);
        darkerBackground = !darkerBackground;


        bool drawWarning = false;
        GUIContent pathLabelContent = new GUIContent(name);

        Color guiColor = GUI.contentColor;
        if (value.Contains("<unsupported>") || value.Contains("<error>"))
        {
            pathLabelContent.tooltip = "Field type cannot be serialized / captured.";
            GUI.contentColor = Color.red;
        }
        else if (explicitlySupportedByApplier)
        {
            pathLabelContent.tooltip = "Field is supported by a custom applier: it will be set via explicit API calls in standalone builds.";
            GUI.contentColor = Color.lightGreen;
        }
        else if (reflectionMayFail)
        {
            drawWarning = true;
            pathLabelContent.tooltip = "Native component field without custom applier: setting this value via reflection may fail.";
            GUI.contentColor = Color.yellowNice;
        }

        EditorGUILayout.BeginHorizontal();
        Rect labelRect = rowRect;
        labelRect.width *= 0.5f;
        EditorGUI.LabelField(labelRect, pathLabelContent, overflowLabel);
        if (drawWarning)
        {
            float iconSize = EditorGUIUtility.singleLineHeight - 5;
            Rect iconRect = new Rect(labelRect.xMin + (EditorGUI.indentLevel - 1) * 15, labelRect.y + 2.5f, iconSize, iconSize);
            GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("console.warnicon").image);
        }
        GUILayout.FlexibleSpace();
        Rect valueRect = rowRect;
        valueRect.xMin = labelRect.xMax;
        EditorGUI.LabelField(valueRect, value, rightAlignedLabel);
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
                    return GetObjectNameFromGuid(guid);
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
        Object obj = ComponentApplierRegistry.GetRegistry().Get(guid);
        if (obj != null) return $"{obj.name} ({obj.GetType().Name})";
        return guid;
    }
    private string GetParentNameFromGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return "<none>";
        GameObject go = ModificationApplier.FindByGuidStatic(guid);
        if (go != null) return go.name;
        return guid;
    }
    #endregion
}
#endif