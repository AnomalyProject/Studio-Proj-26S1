using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PickUpSpawner : MonoBehaviour
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
        if (onEnable)
            SpawnItems();
    }
    private void OnDisable()
    {
        ClearItemsSpawned();
    }
    private void OnDestroy()
    {
        ClearItemsSpawned();
    }


    public void SpawnItems()
    {
        ClearItemsSpawned();

        if (Items.Count == 0 || spawnPoints.Count == 0)
        {
            Debug.Log("Spawner is missing items or spawn points!");
            return;
        }

        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        List<GameObject> itemsToSpawn = new List<GameObject>();

        if (spawnMode == SpawnMode.SpawnAll)
        {
            itemsToSpawn.AddRange(Items);
        }
        else if (spawnMode == SpawnMode.RandomSpawn)
        {
            for (int i = 0; i < randomSpawnAmount; i++)
            {
                int randomItemIndex = Random.Range(0, Items.Count);
                itemsToSpawn.Add(Items[randomItemIndex]);
            }
        }

        int totalToSpawn = Mathf.Min(itemsToSpawn.Count, availablePoints.Count);

        for (int i = 0; i < totalToSpawn; i++)
        {
            int randomPoint = Random.Range(0, availablePoints.Count);
            Transform chosenPoint = availablePoints[randomPoint];

            availablePoints.RemoveAt(randomPoint);

            GameObject newItem = Instantiate(itemsToSpawn[i], chosenPoint.position, chosenPoint.rotation);
            spawnedItems.Add(newItem);
        }
    }

    private void ClearItemsSpawned()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();
    }
}
