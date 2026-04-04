using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SnapshotID))]
public class SnapshotIDEditor : Editor
{
    public override void OnInspectorGUI() { } // Draw nothing — completely hides the component in Inspector
}
#endif

[ExecuteAlways]
[AddComponentMenu("")]
public class SnapshotID : MonoBehaviour
{
    public string guid;

    void Awake()
    {
        if (string.IsNullOrEmpty(guid)) guid = System.Guid.NewGuid().ToString();
    }
}