using System.Collections.Generic;
using UnityEngine;
using PurrNet;

public class PickUpSpawner : NetworkBehaviour
{
    private enum SpawnMode
    {
        SpawnAll,
        RandomSpawn
    }

    [SerializeField] private List<GameObject> Items = new List<GameObject>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [SerializeField] private bool onEnable = false;

    [SerializeField] private SpawnMode spawnMode = SpawnMode.SpawnAll;

    private List<GameObject> spawnedItems = new List<GameObject>();

    [SerializeField] private int randomSpawnAmount;


    private void OnEnable()
    {
        if (onEnable && isServer) SpawnItems();
    }
    private void OnDisable()
    {
        ClearItemsSpawned();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearItemsSpawned();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if(enabled && onEnable && asServer) SpawnItems();
    }


    public void SpawnItems()
    {
        if (!isServer) return;

        ClearItemsSpawned();

        if (Items.Count == 0 || spawnPoints.Count == 0)
        {
            Debug.Log("Spawner is missing items or spawn points!");
            return;
        }

        List<Transform> availablePoints = new List<Transform>(spawnPoints);    //Copy spawnPoints in a temporary list so we can remove used points
        List<GameObject> itemsToSpawn = new List<GameObject>();                //List of prefabs that will actually be spawned

        if (spawnMode == SpawnMode.SpawnAll)
        {
            itemsToSpawn.AddRange(Items);                  //Spawn all items once
        }
        else if (spawnMode == SpawnMode.RandomSpawn)
        {
            for (int i = 0; i < randomSpawnAmount; i++)              //Pick random items from the collection
            {
                int randomItemIndex = Random.Range(0, Items.Count);
                itemsToSpawn.Add(Items[randomItemIndex]);
            }
        }

        int totalToSpawn = Mathf.Min(itemsToSpawn.Count, availablePoints.Count);      //Prevent spawning more items than we have spawn points

        for (int i = 0; i < totalToSpawn; i++)
        {
            int randomPoint = Random.Range(0, availablePoints.Count);
            Transform chosenPoint = availablePoints[randomPoint];

            availablePoints.RemoveAt(randomPoint);                //Remove point so it cannot be used again

            GameObject newItem = Instantiate(itemsToSpawn[i], chosenPoint.position, chosenPoint.rotation, transform);
            if(newItem.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;    //Make sure item doesn't fall through the floor when spawned
            spawnedItems.Add(newItem);                 //Cache spawned item for later cleanup
        }
    }

    private void ClearItemsSpawned()
    {
        if (!isServer) return;

        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        spawnedItems.Clear();
    }
}
