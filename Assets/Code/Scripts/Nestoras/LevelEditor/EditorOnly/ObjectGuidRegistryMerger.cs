#if UNITY_EDITOR
using System.Collections.Generic;
using static ObjectGuidRegistry;
using UnityEditor;
using UnityEngine;

public class ObjectGuidRegistryMerger : EditorWindow
{
    private ObjectGuidRegistry sourceRegistry; // A
    private ObjectGuidRegistry destinationRegistry; // B

    [MenuItem("Window/ObjectGUIDRegistryMerger")]
    private static void ShowWindow() => GetWindow<ObjectGuidRegistryMerger>("ObjectGUIDRegistry Merger");

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("This tool merges an ObjectGUIDRegistry over another. The data of A will be appended into B.\nIf A & B contain conflicts, A will be prioritized, while B will be overriden.\nA is never modified.\nB will be changed and saved.", MessageType.Warning);
        GUILayout.Space(10);

        EditorGUILayout.LabelField("Source ObjectGUIDRegistry (A)", EditorStyles.boldLabel);
        sourceRegistry = (ObjectGuidRegistry)EditorGUILayout.ObjectField(sourceRegistry, typeof(ObjectGuidRegistry), false);
        GUILayout.Space(5);

        EditorGUILayout.LabelField("Destination ObjectGUIDRegistry (B)", EditorStyles.boldLabel);
        destinationRegistry = (ObjectGuidRegistry)EditorGUILayout.ObjectField(destinationRegistry, typeof(ObjectGuidRegistry), false);
        GUILayout.Space(20);

        using (new EditorGUI.DisabledScope(sourceRegistry == null || destinationRegistry == null))
        {
            if (GUILayout.Button("Merge A into B", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Confirm Merge", "This will merge A INTO B.\n\nAny matching GUIDs in B will be replaced by the object reference from A.\n\nContinue?", "Merge", "Cancel")) Merge(sourceRegistry, destinationRegistry);
            }
        }
    }

    private static void Merge(ObjectGuidRegistry source, ObjectGuidRegistry destination)
    {
        Undo.RecordObject(destination, "Merge ObjectGuidRegistry");
        Dictionary<string, Entry> destinationLookup = new Dictionary<string, Entry>();
        foreach (Entry entry in destination.entries) if (!string.IsNullOrEmpty(entry.guid)) destinationLookup[entry.guid] = entry;

        int added = 0;
        int replaced = 0;
        int matches = 0;

        foreach (Entry sourceEntry in source.entries)
        {
            if (string.IsNullOrEmpty(sourceEntry.guid)) continue;

            if (destinationLookup.TryGetValue(sourceEntry.guid, out Entry destinationEntry))
            {
                if (destinationEntry.obj == sourceEntry.obj)
                {
                    matches++;
                    continue; // Skip if the same reference already exists
                }
                destinationEntry.obj = sourceEntry.obj;
                destinationEntry.objName = sourceEntry.objName;
                replaced++;
            }
            else
            {
                destination.entries.Add(new Entry
                {
                    guid = sourceEntry.guid,
                    obj = sourceEntry.obj,
                    objName = sourceEntry.objName,
                });
                added++;
            }
        }

        EditorUtility.SetDirty(destination);
        AssetDatabase.SaveAssets();

        Debug.Log($"ObjectGuidRegistry merge complete.\nAdded: {added}\nReplaced: {replaced}\nMatches: {matches}");
        EditorUtility.DisplayDialog("Merge Complete", $"Merged A INTO B\n\nAdded: {added}\nReplaced: {replaced}\nMatches: {matches}", "OK");
    }
}
#endif