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
        
        float deadline = Time.realtimeSinceStartup + 5f;

        while (!HasUsableSessionData() && Time.realtimeSinceStartup < deadline) yield return null;

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

        int maxPlayers = GetMaxPlayersForPresentation();
        int standeeCount = Mathf.Min(playerSlots.Length, maxPlayers);

        for (int i = 0; i < standeeCount; i++)
        {
            if (playerSlots[i] == null)
            {
                Debug.LogWarning($"[LobbyPresentation] Slot {i} is missing!!");
                continue;
            }

            LobbyStandee standee = Instantiate(standeePrefab, playerSlots[i].position, playerSlots[i].rotation);
            
            if (i < orderedPlayers.Count)
            {
                standee.name = $"LobbyStandee_{orderedPlayers[i].DisplayName}";
                standee.SetupOccupied(orderedPlayers[i]);
            }
            else
            {
                standee.name = $"LobbyStandee_Empty_{i + 1}";
                standee.SetupEmpty();
            }

            activeStandees.Add(standee);
        }
        
    }
    
    private int GetMaxPlayersForPresentation()
    {
        if (SessionManager.Instance == null)
        {
            return 2; // show the smaller room size if sessionManager is null. just a safe fallback
        }

        if (SessionManager.Instance.IsHost && SessionManager.Instance.CurrentSession != null)
        {
            return Mathf.Clamp(SessionManager.Instance.CurrentSession.MaxPlayers, 2, playerSlots.Length);
        }

        ClientSessionData clientSession = SessionManager.Instance.LatestClientSession;

        if (clientSession.Players != null)
        {
            return Mathf.Clamp(clientSession.MaxPlayers, 2, playerSlots.Length);
        }

        return 2;
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
