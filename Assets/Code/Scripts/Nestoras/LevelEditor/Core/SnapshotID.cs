using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;

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
[ExecuteAlways]
[AddComponentMenu("")]
public class SnapshotID : MonoBehaviour
{
    public string guid;
    private void Awake() => EnsureGuid();

#if UNITY_EDITOR
    // Runs in editor when object is created, duplicated, or modified
    private void OnValidate() => EnsureGuid();
#endif

    private void EnsureGuid()
    {
        // Always generate if empty
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString();
            return;
        }

#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(this)) return; // Don't regenerate GUID when making a prefab.

        // Detect duplicates in the scene (Ctrl+D)
        SnapshotID[] all = FindObjectsByType<SnapshotID>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool duplicateExists = all.Any(x => x != this && x.guid == guid);
        if (duplicateExists)
        {
            guid = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this); // Mark dirty so Unity saves the change
        }
#endif
    }
}