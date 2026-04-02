using System;
using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Collections;

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
    
    private readonly Dictionary<PlayerID, NetworkIdentity> spawnedPlayers = new();

    private void Awake()
    {
        // moved spawnPoints on awake because we want to make sure that spawn points exist
        // before anyone else 
        spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
    }

    void Start()
    {
        SpawnExistingPlayers();
    }
    
    private void OnEnable()
    {
        SessionManager.OnServerPlayerAdded += HandlePlayerAdded;
        SessionManager.OnServerPlayerRemoved += HandlePlayerRemoved;
        Debug.Log($"[SpawnManager] Spawn OnEnable run");
    }

    private void OnDisable()
    {
        SessionManager.OnServerPlayerAdded -= HandlePlayerAdded;
        SessionManager.OnServerPlayerRemoved -= HandlePlayerRemoved;
    }

    private void HandlePlayerAdded(PlayerID playerID, ulong steamID, string displayName)
    {
        // only the host can spawn players.
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;
        if (spawnedPlayers.ContainsKey(playerID)) return;

        PlayerID? playerid = SessionManager.Instance.GetPlayerIDForSteam(steamID);
        if (!playerid.HasValue) return;

        SpawnPoint point = GetNextSpawnPoint();
        
        GameObject gameObject = Instantiate(playerPrefab, point.transform.position, point.transform.rotation);
        NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();
        if (networkIdentity == null) return;
        
        spawnedPlayers[playerID] = networkIdentity;
        networkIdentity.GiveOwnership(playerID);

        var nameplate = gameObject.GetComponentInChildren<PlayerNameplate>();
        if (nameplate != null) nameplate.SetName(displayName);

        point.LastUsedTime = Time.time;
        Debug.Log($"[SpawnManager] Spawned {displayName} at {point.name}");
    }
    
    private void HandlePlayerRemoved(PlayerID playerID, ulong steamID, string reason)
    {
        if (!spawnedPlayers.TryGetValue(playerID, out var identity)) return;

        spawnedPlayers.Remove(playerID);

        if (identity != null && identity.isSpawned)
            identity.Despawn();
    }
    
    
    // this fixing a networking timing issue
    private void SpawnExistingPlayers()
    {
        
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;
        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null) return;
        
        Debug.Log($"[SpawnManager] Spawning existing players");
        foreach (var player in SessionManager.Instance.CurrentSession.Players)
        {
            PlayerID? playerid = SessionManager.Instance.GetPlayerIDForSteam(player.SteamID);
            if (playerid.HasValue)
            {
                HandlePlayerAdded(playerid.Value, player.SteamID, player.DisplayName);
            }
               
        }
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
