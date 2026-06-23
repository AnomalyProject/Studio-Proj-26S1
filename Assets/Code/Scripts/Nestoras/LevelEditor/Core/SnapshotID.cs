using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SnapshotID))]
public class SnapshotIDEditor : Editor
{
    public override void OnInspectorGUI() { } // Draw nothing
}
#endif

/// <summary>
/// Nestoras Angelopoulos
/// 
/// A GUID wrapper for <see cref="GameObject"/>s that need to be tracked in <see cref="LevelModification"/>s.
/// </summary>
//[ExecuteAlways]
[AddComponentMenu("")]
public class SnapshotID : MonoBehaviour
{
    public string guid;
    private void Awake() => EnsureGuid();

    private static Dictionary<string, SnapshotID> snapshotIDs = new Dictionary<string, SnapshotID>();

#if UNITY_EDITOR
    // Runs in editor when object is created, duplicated, or modified
    private void OnValidate() => EnsureGuid();

    private bool listMarkedForDeletion = false;
    private void ResetGUIDsNextFrame()
    {
        snapshotIDs.Clear();
        listMarkedForDeletion = false;
    }
#endif

    private void EnsureGuid()
    {
        // Always generate if empty
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString();
            snapshotIDs.Add(guid, this);
            return;
        }

#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(this)) return; // Don't regenerate GUID when making a prefab.
        if (EditorApplication.isPlayingOrWillChangePlaymode) return; // Don't regenerate GUID when entering playmode.

        // Detect duplicates in the scene (Ctrl+D)
        if (snapshotIDs.Count == 0)
        {
            if (!listMarkedForDeletion) EditorApplication.delayCall += ResetGUIDsNextFrame;
            listMarkedForDeletion = true;
            try
            {
                snapshotIDs.AddRange(FindObjectsByType<SnapshotID>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToDictionary(x => x.guid));
            }
            catch // Duplicates Occured
            {
                guid = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(this); // Mark dirty so Unity saves the change
                snapshotIDs.Clear(); // Scan again in next object
                return;
            }
        }

        try // snapshotIDs could be uninitialized
        {
            bool duplicateExists = snapshotIDs[guid] != this;
            if (duplicateExists)
            {
                guid = System.Guid.NewGuid().ToString();
                snapshotIDs.Add(guid, this);
                EditorUtility.SetDirty(this); // Mark dirty so Unity saves the change
            }
        }
        catch { }
#endif
    }
}