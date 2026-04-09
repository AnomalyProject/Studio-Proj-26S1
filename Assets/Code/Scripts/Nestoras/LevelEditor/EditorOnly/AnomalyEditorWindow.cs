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

            Debug.Log(lastDiff.ToString());
        }

        if (GUILayout.Button("Save Variation Asset"))
        {
            if (lastDiff == null)
            {
                Debug.LogError("No diff to save.");
                return;
            }

            LevelModification variation = BuildLevelModification(lastDiff);

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Variation",
                "NewVariation",
                "asset",
                "Choose location"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(variation, path);
                AssetDatabase.SaveAssets();
                Debug.Log("Variation saved!");
            }
        }

        GUI.enabled = true;

        EditorGUILayout.Space();
    }
}
#endif