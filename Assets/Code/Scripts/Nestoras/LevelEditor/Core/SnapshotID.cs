using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

//[CustomEditor(typeof(SnapshotID))]
//public class SnapshotIDEditor : Editor
//{
//    public override void OnInspectorGUI() { } // Draw nothing
//}
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
    private static Dictionary<string, SnapshotID> snapshotIDs = new Dictionary<string, SnapshotID>();

    public string guid;
    private void Awake() => EnsureGuid();
    private void OnDestroy() => snapshotIDs.Remove(guid);

#if UNITY_EDITOR
    // Runs in editor when object is created, duplicated, or modified
    private void OnValidate() => EnsureGuid();
    [InitializeOnLoadMethod] private static void ClearGuidMap() => snapshotIDs.Clear();
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
        if (EditorApplication.isPlaying) return; // Don't regenerate GUIDs when in playmode.

        // Detect duplicates in the scene (Ctrl+D)
        if (!snapshotIDs.TryGetValue(guid, out SnapshotID cached)) snapshotIDs.Add(guid, this);
        else if (cached != this)
        {
            guid = System.Guid.NewGuid().ToString();
            snapshotIDs.Add(guid, this);
            EditorUtility.SetDirty(this); // Mark dirty so Unity saves the change
        }
#endif
    }
}