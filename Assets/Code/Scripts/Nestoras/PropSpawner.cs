using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Script to spawn a prefab into the level.
/// Meant to be used with a ModificationApplier.
/// </summary>
public class PropSpawner : MonoBehaviour
{
    [SerializeField] private GameObject propToSpawn;
    [SerializeField] private Transform spawnPosition;
    private GameObject spawnedProp;
    public void SpawnAtSpawnPosition() => spawnedProp = Instantiate(propToSpawn, spawnPosition.position, spawnPosition.rotation);
    public void DespawnObject()
    {
        if (spawnedProp != null) Destroy(spawnedProp);
    }
}
