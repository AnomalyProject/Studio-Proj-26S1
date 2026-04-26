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
        if (SessionManager.Instance == null) return false;

        return SessionManager.Instance.LatestClientSession.Players != null;
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

        if (SessionManager.Instance == null) return;

        if (SessionManager.Instance == null) return;

        ClientSessionData sessionData = SessionManager.Instance.LatestClientSession;
        if (sessionData.Players == null) return;

        List<ClientPlayerInfo> orderedPlayers = BuildOrderedPlayerList(sessionData);

        int standeeCount = Mathf.Min(playerSlots.Length, orderedPlayers.Count);

        for (int i = 0; i < standeeCount; i++)
        {
            LobbyStandee standee = Instantiate(standeePrefab, playerSlots[i]);
            standee.transform.localPosition = Vector3.zero;
            standee.transform.localRotation = Quaternion.identity;
            standee.Setup(orderedPlayers[i]);

            activeStandees.Add(standee);
        }
    }
    
    private List<ClientPlayerInfo> BuildOrderedPlayerList(ClientSessionData sessionData)
    {
        List<ClientPlayerInfo> orderedPlayers = new List<ClientPlayerInfo>();

        // puttin the host to the first slot (slot 0)
        for (int i = 0; i < sessionData.Players.Count; i++)
        {
            if (sessionData.Players[i].IsHost)
            {
                orderedPlayers.Add(sessionData.Players[i]);
            }
        }

        for (int i = 0; i < sessionData.Players.Count; i++)
        {
            if (!sessionData.Players[i].IsHost)
            {
                orderedPlayers.Add(sessionData.Players[i]);
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
