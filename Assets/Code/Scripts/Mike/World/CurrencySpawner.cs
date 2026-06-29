using System.Linq;
using UnityEngine;

public class CurrencySpawner : PickUpSpawner
{
    [SerializeField] private AnomalyManager anomalyManager;

    private void Awake() => anomalyManager.OnMapChanged += SpawnCurrency;
    private async void SpawnCurrency(GameMap map)
    {
        if (!isServer) return;

        await Awaitable.NextFrameAsync(); // Let the map move.
        spawnPoints = map.CurrencySpawnPoints.ToList();
        SpawnItems();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) spawnPoints.Clear();

        spawnMode = SpawnMode.RandomSpawn;
        onEnable = false;
    }
}