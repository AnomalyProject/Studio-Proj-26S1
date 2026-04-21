using UnityEngine;

public class GameMap : MonoBehaviour
{
    [SerializeField] Transform entryPointAnchor, exitPointAnchor;
    public Transform EntryPointAnchor => entryPointAnchor;
    public Transform ExitPointAnchor => exitPointAnchor;
    bool HasAnchorPoints => exitPointAnchor != null && entryPointAnchor != null;

    protected virtual void Awake()
    {
        // Ensure the entry anchor is parent to the map
        Transform newEntryAnchor = new GameObject($"Entry Anchor ({name})").transform;
        newEntryAnchor.position = entryPointAnchor.transform.position;
        newEntryAnchor.rotation = entryPointAnchor.transform.rotation;
        entryPointAnchor.gameObject.SetActive(false);
        exitPointAnchor.gameObject.SetActive(false);

        transform.SetParent(newEntryAnchor.transform, true);
        entryPointAnchor = newEntryAnchor;
    }

    private void OnValidate()
    {
        if (!HasAnchorPoints)
        {
            Debug.LogWarning("You must assign entry & exit point anchors to the anomaly map!");
            return;
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying || !HasAnchorPoints) return;
    
        if (entryPointAnchor.position != Vector3.zero)
        {
            Debug.LogWarning("Entry point must exist in the center of the world!");
            entryPointAnchor.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        entryPointAnchor.transform.localScale = Vector3.one;
        exitPointAnchor.transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        if(entryPointAnchor.gameObject != null) Destroy(entryPointAnchor.gameObject);
    }
}
