using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class LobbyPresentationManager : MonoBehaviour
{
    [SerializeField] private Transform[] playerSlots;
    [SerializeField] private LobbyStandee standeePrefab;
    
    private readonly List<LobbyStandee> activeStandees = new();
    private Coroutine waitForSessionRoutine;
    
    private void OnEnable()
    {
        // todelete
        Debug.Log("[LobbyPresentation] OnEnable called.");
        
        SessionEvents.OnSessionDataChanged += RefreshPresentation;
        
        if (waitForSessionRoutine != null)
        {
            StopCoroutine(waitForSessionRoutine);
        }
        
        waitForSessionRoutine = StartCoroutine(WaitForSessionThenRefresh());
    }

    private void OnDisable()
    {

        SessionEvents.OnSessionDataChanged -= RefreshPresentation;
        
        if (waitForSessionRoutine != null)
        {
            StopCoroutine(waitForSessionRoutine);
            waitForSessionRoutine = null;
        }
        
        ClearPresentation();
    }
    
    private IEnumerator WaitForSessionThenRefresh()
    {
        //todelete
        Debug.Log("[LobbyPresentation] Waiting for session data.");
        
        float deadline = Time.realtimeSinceStartup + 5f;

        while (!HasUsableSessionData() && Time.realtimeSinceStartup < deadline) yield return null;
        
        //todelete
        if (!HasUsableSessionData())
        {
            Debug.LogWarning("[LobbyPresentation] Timed out waiting for session data.");
        }
        else
        {
            Debug.Log("[LobbyPresentation] Session data found.");
        }

        RefreshPresentation();
        waitForSessionRoutine = null;
    }
    
    private bool HasUsableSessionData()
    {
        List<ClientPlayerInfo> players = GetPlayersForPresentation();
        return players.Count > 0;
    }

    
    private void RefreshPresentation()
    {
       //todelete
        Debug.Log("[LobbyPresentation] RefreshPresentation called.");

        ClearPresentation();

        if (standeePrefab == null)
        {
            Debug.LogWarning("[LobbyPresentation] Standee prefab is missing.");
            return;
        }

        if (playerSlots == null || playerSlots.Length == 0)
        {
            Debug.LogWarning("[LobbyPresentation] Player slots are missing.");
            return;
        }

        List<ClientPlayerInfo> orderedPlayers = GetPlayersForPresentation();
        Debug.Log($"[LobbyPresentation] Players found: {orderedPlayers.Count}");

        int standeeCount = Mathf.Min(playerSlots.Length, orderedPlayers.Count);

        for (int i = 0; i < standeeCount; i++)
        {
            if (playerSlots[i] == null)
            {
                Debug.LogWarning($"[LobbyPresentation] Slot {i} is missing.");
                continue;
            }

            LobbyStandee standee = Instantiate(standeePrefab, playerSlots[i].position, playerSlots[i].rotation);
            standee.name = $"LobbyStandee_{orderedPlayers[i].DisplayName}";
            standee.Setup(orderedPlayers[i]);

            activeStandees.Add(standee);

            Debug.Log($"[LobbyPresentation] Spawned standee for {orderedPlayers[i].DisplayName} at {playerSlots[i].position}");
        }
    }
    
    private List<ClientPlayerInfo> GetPlayersForPresentation()
    {
        List<ClientPlayerInfo> players = new List<ClientPlayerInfo>();

        if (SessionManager.Instance == null)
        {
            return players;
        }

        if (SessionManager.Instance.IsHost && SessionManager.Instance.CurrentSession != null)
        {
            for (int i = 0; i < SessionManager.Instance.CurrentSession.Players.Count; i++)
            {
                PlayerSessionInfo serverPlayer = SessionManager.Instance.CurrentSession.Players[i];

                players.Add(new ClientPlayerInfo
                {
                    SteamID = serverPlayer.SteamID,
                    DisplayName = serverPlayer.DisplayName,
                    IsReady = serverPlayer.IsReady,
                    IsHost = serverPlayer.IsHost
                });
            }

            return BuildOrderedPlayerList(players);
        }

        ClientSessionData clientSession = SessionManager.Instance.LatestClientSession;

        if (clientSession.Players != null)
        {
            return BuildOrderedPlayerList(clientSession.Players);
        }

        return players;
    }
    
    private List<ClientPlayerInfo> BuildOrderedPlayerList(List<ClientPlayerInfo> players)
    {
        List<ClientPlayerInfo> orderedPlayers = new List<ClientPlayerInfo>();

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsHost)
            {
                orderedPlayers.Add(players[i]);
            }
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].IsHost)
            {
                orderedPlayers.Add(players[i]);
            }
        }

        return orderedPlayers;
    }

    private void ClearPresentation()
    {
        for (int i = 0; i < activeStandees.Count; i++)
        {
            Destroy(activeStandees[i].gameObject);
        }

        activeStandees.Clear();
    }
    
}
