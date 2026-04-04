using UnityEngine;

public class GameMap : MonoBehaviour
{
    [SerializeField] Transform entryPointAnchor, exitPointAnchor;
    [SerializeField] GameObject baseMap;
    public GameObject BaseMap => baseMap;
    public Transform EntryPointAnchor => entryPointAnchor;
    public Transform ExitPointAnchor => exitPointAnchor;

    protected virtual void Awake()
    {
        // Ensure the entry anchor is parent to the map
        Transform newEntryAnchor = new GameObject($"Entry Anchor ({name})").transform;
        newEntryAnchor.position = entryPointAnchor.transform.position;
        newEntryAnchor.rotation = entryPointAnchor.transform.rotation;

        transform.SetParent(newEntryAnchor.transform, true);
        entryPointAnchor = newEntryAnchor;
    }

    private void OnDestroy()
    {
        if(entryPointAnchor.gameObject != null) Destroy(entryPointAnchor.gameObject);
    }
}
