using System.Threading.Tasks;
using UnityEngine;

public class RaycastSpawner : PlayerItem, IInteractable<PlayerBody>
{
    #region Serialized
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private MeshRenderer previewObject;
    [SerializeField, Min(0.5f)] private float rayDistance = 3f;
    [SerializeField, Min(0)] private float spawnCooldown = 1f;
    [SerializeField] private Vector3 boundsSize = new Vector3(1f, 2f, 1f);
    [SerializeField] private Vector3 boundsOffset = new Vector3(0, 1f, 0);
    [SerializeField] LayerMask boundsCheckLayer;
    [SerializeField] Color validColor = Color.lightGreen, invalidColor = Color.softRed;
    #endregion

    #region Cached
    private Vector3 spawnPosition;
    private bool isValidSpawn;
    private float currentCooldown;
    #endregion

    Quaternion spawnRotation => Quaternion.Euler(0, transform.eulerAngles.y, 0);

    protected override void OnSpawned()
    {
        base.OnSpawned();
        enabled = isOwner;
    }

    private void Update() => UpdatePosition();
    public Task<bool> CanInteract(PlayerBody interactor) => Task.FromResult(true);
    public Task<bool> TryInteract(PlayerBody interactor)
    {
        if (!isValidSpawn || currentCooldown > 0) return Task.FromResult(false);

        currentCooldown = spawnCooldown;
        previewObject.enabled = false;
        Instantiate(spawnPrefab, spawnPosition, spawnRotation);
        return Task.FromResult(true);
    }

    private void UpdatePosition()
    {
        if (!isOwner) return;

        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
            return;
        }

        spawnPosition = CalculateSpawnPosition(out isValidSpawn);

        if (!previewObject) return;
        previewObject.transform.position = spawnPosition;
        previewObject.transform.rotation = spawnRotation;
        previewObject.enabled = spawnPosition != Vector3.zero;

        foreach (var mat in previewObject.materials) mat.color = isValidSpawn? validColor : invalidColor;
    }

    private Vector3 CalculateSpawnPosition(out bool isValid)
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit forwardHit, rayDistance))
        {
            Vector3 grounded = FindGroundedPosition(forwardHit.point, out isValid);
            if (grounded != Vector3.zero) return grounded;
        }

        Vector3 projectedPoint = transform.position + transform.forward * rayDistance;
        Vector3 fallback = FindGroundedPosition(projectedPoint, out isValid);

        if (fallback != Vector3.zero) return fallback;

        isValid = false;
        return Vector3.zero;
    }

    private Vector3 FindGroundedPosition(Vector3 startPoint, out bool isValid)
    {
        isValid = false;

        if (!Physics.Raycast(startPoint + Vector3.up, Vector3.down, out RaycastHit groundHit, Mathf.Infinity)) return Vector3.zero;

        Vector3 landingPoint = groundHit.point;
        Vector3 boundsCenter = landingPoint + boundsOffset;

        bool hasCeiling = Physics.Raycast(landingPoint, Vector3.up, out RaycastHit ceilingHit, Mathf.Infinity);
        bool enoughHeight = !hasCeiling || ceilingHit.distance >= boundsSize.y;
        bool areaClear = IsAreaClear(boundsCenter, groundHit.collider);

        isValid = enoughHeight && areaClear;

        return landingPoint;
    }

    private bool IsAreaClear(Vector3 center, Collider groundCollider)
    {
        Collider[] overlaps = Physics.OverlapBox(
            center,
            boundsSize * 0.5f,
            Quaternion.identity,
            boundsCheckLayer, // Check all layers
            QueryTriggerInteraction.Ignore);

        foreach (var col in overlaps)
        {
            if (col.transform == previewObject) continue;
            if (col == groundCollider) continue;         
            return false;                                
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        if(!previewObject) return;
        Gizmos.color = isValidSpawn? Color.green : Color.red;
        Gizmos.DrawWireCube(previewObject.transform.position + boundsOffset, boundsSize);
    }
}