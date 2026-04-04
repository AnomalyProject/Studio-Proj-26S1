#if UNITY_EDITOR
using System.Collections.Generic;
using static SnapshotUtility;
using UnityEngine;
using UnityEditor;

public class AnomalyEditorWindow : EditorWindow
{
    private GameObject root; // Root of the hierarchy being tracked

    private List<GameObjectSnapshot> baseline;
    private DiffResult lastDiff;

    private Vector2 scroll;

    [MenuItem("Window/Anomaly Editor")]
    public static void Open() => GetWindow<AnomalyEditorWindow>("Anomaly Editor");

    private void OnGUI()
    {
        GUILayout.Label("Anomaly Level Editor", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // User selects a root GameObject manually (not tied to current selection)
        root = (GameObject)EditorGUILayout.ObjectField("Root Object", root, typeof(GameObject), true);

        EditorGUILayout.Space();

        GUI.enabled = root != null;

        // Capture initial state of the hierarchy
        if (GUILayout.Button("Capture Baseline"))
        {
            baseline = Capture(root);
            lastDiff = null;

            Debug.Log($"Baseline captured: {baseline.Count} objects");
        }

        // Compare current scene state against baseline
        if (GUILayout.Button("Compare With Current"))
        {
            if (baseline == null) Debug.LogError("Capture a baseline first.");
            else lastDiff = Diff(baseline, Capture(root));
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        // Render diff results in UI
        DrawDiff();
    }

    private void DrawDiff()
    {
        if (lastDiff == null) return;

        GUILayout.Label("Diff Results", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        // -------- ADDED OBJECTS --------
        GUILayout.Label($"Added ({lastDiff.added.Count})", EditorStyles.boldLabel);
        foreach (GameObjectSnapshot a in lastDiff.added) EditorGUILayout.LabelField("+ " + a.path);

        EditorGUILayout.Space();

        // -------- REMOVED OBJECTS --------
        GUILayout.Label($"Removed ({lastDiff.removed.Count})", EditorStyles.boldLabel);
        foreach (GameObjectSnapshot r in lastDiff.removed) EditorGUILayout.LabelField("- " + r.path);

        EditorGUILayout.Space();

        // -------- CHANGED OBJECTS --------
        GUILayout.Label($"Changed ({lastDiff.changed.Count})", EditorStyles.boldLabel);

        foreach ((GameObjectSnapshot before, GameObjectSnapshot after) c in lastDiff.changed)
        {
            // Object path label
            EditorGUILayout.LabelField("* " + c.before.path);

            EditorGUI.indentLevel++;

            // Active state changes
            if (c.before.active != c.after.active) EditorGUILayout.LabelField($"Active: {c.before.active} -> {c.after.active}");

            // Compute component-level diff
            ComponentDiff compDiff = DiffComponents(c.before.components, c.after.components);

            // -------- Added Components --------
            if (compDiff.added.Count > 0)
            {
                EditorGUILayout.LabelField("Added Components:", EditorStyles.boldLabel);

                foreach (ComponentSnapshot comp in compDiff.added)
                {
                    EditorGUILayout.LabelField($"+ {comp.type}");

                    EditorGUI.indentLevel++;
                    foreach (KeyValuePair<string, TypedValue> kv in comp.properties) EditorGUILayout.LabelField($"{kv.Key}: {kv.Value.value}");
                    EditorGUI.indentLevel--;
                }
            }

            // -------- Removed Components --------
            if (compDiff.removed.Count > 0)
            {
                EditorGUILayout.LabelField("Removed Components:", EditorStyles.boldLabel);

                foreach (ComponentSnapshot comp in compDiff.removed) EditorGUILayout.LabelField($"- {comp.type}");
            }

            // -------- Modified Components --------
            if (compDiff.modified.Count > 0)
            {
                EditorGUILayout.LabelField("Modified Components:", EditorStyles.boldLabel);

                foreach ((ComponentSnapshot before, ComponentSnapshot after) in compDiff.modified)
                {
                    EditorGUILayout.LabelField($"* {before.type}");

                    EditorGUI.indentLevel++;

                    // Enabled toggle differences
                    if (before.enabled != after.enabled) EditorGUILayout.LabelField($"Enabled: {before.enabled} -> {after.enabled}");

                    // Property-by-property comparison
                    foreach (KeyValuePair<string, TypedValue> kv in before.properties)
                    {
                        if (!after.properties.TryGetValue(kv.Key, out TypedValue newVal)) continue;
                        if (!Equals(kv.Value.value, newVal.value)) EditorGUILayout.LabelField($"{kv.Key}: {kv.Value.value} -> {newVal.value}");
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif