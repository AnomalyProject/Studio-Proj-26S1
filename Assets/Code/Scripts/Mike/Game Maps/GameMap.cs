using UnityEngine;

public class GameMap : MonoBehaviour
{
    [SerializeField] Transform entryPointAnchor, exitPointAnchor;
    public Transform EntryPointAnchor => entryPointAnchor;
    public Transform ExitPointAnchor => exitPointAnchor;
    bool HasAnchorPoints => exitPointAnchor != null && entryPointAnchor != null;

    protected virtual void Awake()
    {
        entryPointAnchor.gameObject.SetActive(false);
        exitPointAnchor.gameObject.SetActive(false);
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
}
