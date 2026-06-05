using System.Collections.Generic;
using UnityEngine;
using PurrNet;

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
    
    private readonly Dictionary<ulong, NetworkIdentity> spawnedPlayers = new();
    private readonly Dictionary<ulong, PlayerID> currentPlayerIds = new();

    //See what spawn point is assigned to what player
    private readonly Dictionary<ulong, SpawnPoint> playerSpawnPoints = new();
    private readonly HashSet<SpawnPoint> occupiedPoints = new();

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
        SessionManager.OnServerSessionChanged += RestoreConnectedPlayerBodies;
        Debug.Log($"[SpawnManager] Spawn OnEnable run");
    }

    private void OnDisable()
    {
        SessionManager.OnServerPlayerAdded -= HandlePlayerAdded;
        SessionManager.OnServerPlayerRemoved -= HandlePlayerRemoved;
        SessionManager.OnServerSessionChanged -= RestoreConnectedPlayerBodies;
    }

    private void HandlePlayerAdded(PlayerID playerID, ulong steamID, string displayName)
    {
        RestoreOrSpawnPlayer(playerID, steamID, displayName);
    }
    
    private void RestoreConnectedPlayerBodies()
    {
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;
        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null) return;

        foreach (PlayerSessionInfo player in SessionManager.Instance.CurrentSession.Players)
        {
            if (!player.IsConnected) continue;

            PlayerID? playerID = SessionManager.Instance.GetPlayerIDForSteam(player.SteamID);
            if (!playerID.HasValue) continue;

            RestoreOrSpawnPlayer(playerID.Value, player.SteamID, player.DisplayName);
        }
    }
    
    private void RestoreOrSpawnPlayer(PlayerID playerID, ulong steamID, string displayName)
    {
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;

        currentPlayerIds[steamID] = playerID;

        if (spawnedPlayers.TryGetValue(steamID, out NetworkIdentity oldIdentity))
        {
            if (oldIdentity != null && oldIdentity.isSpawned)
            {
                oldIdentity.GiveOwnership(playerID);
                ApplyPlayerName(oldIdentity.gameObject, steamID, displayName);
                Debug.Log($"[SpawnManager] Restored ownership for {displayName}");
                return;
            }

            spawnedPlayers.Remove(steamID);
        }

        SpawnPoint point = GetSpawnPointForPlayer(steamID);

        if (point == null)
        {
            Debug.LogError($"[SpawnManager] No spawn point available for {displayName}. Cannot spawn player.");
            return;
        }

        GameObject gameObject = Instantiate(playerPrefab, point.transform.position, point.transform.rotation);
        NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();

        if (networkIdentity == null)
        {
            Debug.LogError("[SpawnManager] Player prefab missing NetworkIdentity!");
            return;
        }

        spawnedPlayers[steamID] = networkIdentity;
        networkIdentity.GiveOwnership(playerID);
        ApplyPlayerName(gameObject, steamID, displayName);

        Debug.Log($"[SpawnManager] Spawned {displayName} at {point.name}");
    }
    
    private void HandlePlayerRemoved(PlayerID playerID, ulong steamID, string reason)
    {
        currentPlayerIds.Remove(steamID);
        
        //Free his spawn point before sending him home
        if(playerSpawnPoints.TryGetValue(steamID, out SpawnPoint freedPoint))
        {
            playerSpawnPoints.Remove(steamID);
            occupiedPoints.Remove(freedPoint);
            Debug.Log($"[SpawnManager] Spawn point is now free -> {freedPoint.name} <-");
        }

        if (!spawnedPlayers.TryGetValue(steamID, out var identity)) return;

        spawnedPlayers.Remove(steamID);

        if (identity != null && identity.isSpawned) identity.Despawn();
    }
    
    
    // this fixing a networking timing issue
    private void SpawnExistingPlayers()
    {
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;
        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null) return;
        
        Debug.Log($"[SpawnManager] Spawning existing players");
        foreach (var player in SessionManager.Instance.CurrentSession.Players)
        {
            if (!player.IsConnected) continue;
            
            PlayerID? playerid = SessionManager.Instance.GetPlayerIDForSteam(player.SteamID);
            if (playerid.HasValue)
            {
                HandlePlayerAdded(playerid.Value, player.SteamID, player.DisplayName);
            }
        }
    }
    
    private SpawnPoint GetNextSpawnPoint()
    {
        if(spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[SpawnManager] You forgot to put the spawn points in the scene!!!");
            return null;
        }

        //Get all free points
        List<SpawnPoint> availablePoints = new List<SpawnPoint>();
        
        /*foreach (var pipi in spawnPoints)
        {
            if (!playerSpawnPoints.ContainsValue(pipi))
            {
                availablePoints.Add(pipi);
            }
        }*/
        
        // note: changed the Dictionary(O(n)) to a HashSet (O(1)) because it's a better data structure for networking.
        // Network code needs to be fast and predictable. If we have 6 points and 6 players that's 8 × 6 = 48 comparisons.
        // It works for small numbers but it's the wrong data structure for the job.
        foreach (var point in spawnPoints)
        {
            if (!occupiedPoints.Contains(point))
            {
                availablePoints.Add(point);
            }
        }

        //If all points are not free, find the one thats the least recent
        if(availablePoints.Count == 0)
        {
            SpawnPoint oldPoint = spawnPoints[0];
            for (int i = 1; i < spawnPoints.Length; i++)
            {
                if(spawnPoints[i].LastUsedTime < oldPoint.LastUsedTime)
                {
                    oldPoint = spawnPoints[i];
                }
            }
            Debug.Log($"[SpawnManager] No free points, so we use the least recent one");
            return oldPoint;
        }

        //Random strat
        if (spawnStrategy == SpawnStrategy.Random)
        {
            SpawnPoint selected = availablePoints[UnityEngine.Random.Range(0, availablePoints.Count)];
            Debug.Log($"[SpawnManager] Random free spawn selected: {selected.name}");
            return selected;
        }

        //Byturn strat
        for(int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnPoint point = spawnPoints[byTurnIndex];
            byTurnIndex = (byTurnIndex + 1) % spawnPoints.Length;

            if (availablePoints.Contains(point))
            {
                Debug.Log($"[SpawnManager] Free byTurn spawn: {point.name}");
                return point;
            }
        }

        return availablePoints[0];
    }
    
    private SpawnPoint GetSpawnPointForPlayer(ulong steamID)
    {
        if (playerSpawnPoints.TryGetValue(steamID, out SpawnPoint point) && point != null)
        {
            return point;
        }

        point = GetNextSpawnPoint();

        if (point != null)
        {
            playerSpawnPoints[steamID] = point;
            occupiedPoints.Add(point);
            point.LastUsedTime = Time.time;
        }

        return point;
    }

    private void ApplyPlayerName(GameObject playerObject, ulong steamID, string displayName)
    {
        PlayerSessionInfo? playerInfo = SessionManager.Instance.CurrentSession.GetPlayer(steamID);
        PlayerNameplate nameplate = playerObject.GetComponentInChildren<PlayerNameplate>();

        if (nameplate != null && playerInfo.HasValue)
        {
            nameplate.SetName(displayName, playerInfo.Value.ColorIndex);
        }
    }
    
}
