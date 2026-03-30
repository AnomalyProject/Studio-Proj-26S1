using System;
using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public enum SpawnStrategy
{
    ByTurn,
    Random
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab; 
    [SerializeField] private SpawnStrategy spawnStrategy = SpawnStrategy.ByTurn;
    
    private SpawnPoint[] spawnPoints;
    private int byTurnIndex = 0;
    
    void Start()
    {
        spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);    
    }

    private void OnEnable()
    {
        SessionEvents.OnPlayerJoined += HandlePlayerJoined;
    }

    private void OnDisable()
    {
        SessionEvents.OnPlayerJoined -= HandlePlayerJoined;
    }

    private void HandlePlayerJoined(ulong steamID, string displayName)
    {
        // only the host can spawn players.
        if (NetworkManager.main == null || NetworkManager.main.isServer) return;

        PlayerID? playerID = SessionManager.Instance.GetPlayerIDForSteam(steamID);
        if (!playerID.HasValue) return;

        SpawnPoint point = GetNextSpawnPoint();
        GameObject gameObject = Instantiate(playerPrefab, point.transform.position, Quaternion.identity);   
        gameObject.GetComponent<NetworkIdentity>().GiveOwnership(playerID.Value);
        
        point.LastUsedTime   = Time.time;
        Debug.Log($"[SpawnManager] Spawned {displayName} at {point.name}]");
    }
    
    private SpawnPoint GetNextSpawnPoint()
    {
        if (spawnStrategy == SpawnStrategy.Random)
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        
        SpawnPoint point = spawnPoints[byTurnIndex];
        byTurnIndex = (byTurnIndex + 1) % spawnPoints.Length;
        return point;
    }
    
}
