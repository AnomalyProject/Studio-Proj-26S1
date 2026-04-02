using System.Collections.Generic;
using UnityEngine;
using PurrNet;
using Steamworks;

public class NetworkTestUI : MonoBehaviour
{
    private readonly List<string> eventLog = new();
    
    private HostStartupStatus hostStartupStatus;
    private bool hasHostStartupStatus;
    
    private JoinStartupStatus joinStartupStatus;
    private bool hasJoinStartupStatus;


    private const int MaxLogEntries = 5;

    private void OnEnable()
    {
        SessionEvents.OnPlayerJoined += LogPlayerJoined;
        SessionEvents.OnPlayerLeft += LogPlayerLeft;
        SessionEvents.OnSessionDataChanged += LogSessionDataChanged;
        SessionEvents.OnSessionError += LogSessionError;
        SteamSessionBridge.Instance.OnHostStartupStatusChanged += HandleHostStartupStatusChanged;
        hostStartupStatus = SteamSessionBridge.Instance.CurrentHostStartupStatus;
        hasHostStartupStatus = true;
        
        SteamSessionBridge.Instance.OnJoinStartupStatusChanged += HandleJoinStartupStatusChanged;
        joinStartupStatus = SteamSessionBridge.Instance.CurrentJoinStartupStatus;
        hasJoinStartupStatus = true;

    }


    private void OnDisable()
    {
        SessionEvents.OnPlayerJoined -= LogPlayerJoined;
        SessionEvents.OnPlayerLeft -= LogPlayerLeft;
        SessionEvents.OnSessionDataChanged -= LogSessionDataChanged;
        SessionEvents.OnSessionError -= LogSessionError;

        SteamSessionBridge.Instance.OnHostStartupStatusChanged -= HandleHostStartupStatusChanged;
        SteamSessionBridge.Instance.OnJoinStartupStatusChanged -= HandleJoinStartupStatusChanged;


    }

    private void AddLog(string message)
    {
        eventLog.Add(message);
        if (eventLog.Count > MaxLogEntries)
            eventLog.RemoveAt(0);
    }

    private void LogPlayerJoined(ulong steamId, string displayName)
    {
        AddLog($"JOINED: {displayName} ({steamId})");
    }

    private void LogPlayerLeft(ulong steamId, string reason)
    {
        AddLog($"LEFT: {steamId} ({reason})");
    }

    private void LogSessionDataChanged()
    {
        AddLog("SESSION DATA UPDATED");
    }

    private void LogSessionError(SessionErrorResponse error)
    {
        AddLog($"ERROR: {error.Code} - {error.Message}");
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 400));

        bool isConnected = NetworkManager.main != null &&
            (NetworkManager.main.isClient || NetworkManager.main.isServer);

        if (!isConnected)
        {
            DrawDisconnectedUI();
            DrawHostDiagnostics();
            DrawJoinDiagnostics();

        }
        else
        {
            DrawConnectedUI();
        }

        DrawEventLog();

        GUILayout.EndArea();
    }

 

    private void DrawDisconnectedUI()
    {
        GUILayout.Label("=== Not Connected ===");

        if (GUILayout.Button("HOST"))
        {
            SteamSessionBridge.Instance.BeginSteamListenHost();
        }

        if (GUILayout.Button("JOIN"))
        {
            NetworkManager.main.StartClient();
        }
    }

    private void DrawConnectedUI()
    {
        bool isHost = NetworkManager.main.isHost;
        GUILayout.Label($"=== Connected as {(isHost ? "HOST" : "CLIENT")} ===");

        if (GUILayout.Button("Disconnect"))
        {
            if (isHost)
            {
                NetworkManager.main.StopClient();
                NetworkManager.main.StopServer();                
            }
            else
            {
                NetworkManager.main.StopClient();
            }
                
        }

        GUILayout.Label("--- RPC Tests ---");

        if (GUILayout.Button("Request Join"))
        {
            SessionManager.Instance.RequestJoinSession(
                SteamUser.GetSteamID().m_SteamID,
                SteamFriends.GetPersonaName()
            );
        }

        if (GUILayout.Button("Toggle Ready"))
        {
            SessionManager.Instance.RequestToggleReady();
        }

        if (GUILayout.Button("Request Leave"))
        {
            SessionManager.Instance.RequestLeaveSession();
        }

        if (GUILayout.Button("Start Match"))
        {
            SessionManager.Instance.RequestStartMatch();
        }
    }
    
    private void DrawHostDiagnostics()
    {
        GUILayout.Space(10);
        GUILayout.Label("=== Host Diagnostics ===");

        if (!hasHostStartupStatus)
        {
            GUILayout.Label("No host startup status yet.");
            return;
        }

        GUILayout.Label($"Stage: {hostStartupStatus.Stage}");
        GUILayout.Label($"Failure Stage: {hostStartupStatus.FailureStage}");
        GUILayout.Label($"Transport: {hostStartupStatus.ActiveTransport}");
        GUILayout.Label($"Attempt: {hostStartupStatus.AttemptID}");
        GUILayout.Label($"Message: {hostStartupStatus.Message}");
    }
    
    private void DrawJoinDiagnostics()
    {
        // todo:delete this dummy comment
        GUILayout.Space(10);
        GUILayout.Label("--- Join Diagnostics ---");

        if (!hasJoinStartupStatus)
        {
            GUILayout.Label("No join status yet.");
            return;
        }

        GUILayout.Label($"Stage: {joinStartupStatus.Stage}");
        GUILayout.Label($"Failure Source: {joinStartupStatus.FailureSource}");
        GUILayout.Label($"Attempt: {joinStartupStatus.AttemptID}");
        GUILayout.Label($"Target Lobby: {joinStartupStatus.TargetLobbyId}");
        GUILayout.Label($"Message: {joinStartupStatus.Message}");
    }
    
    private void DrawEventLog()
    {
        GUILayout.Label("--- Event Log ---");

        for (int i = 0; i < eventLog.Count; i++)
        {
            GUILayout.Label(eventLog[i]);
        }
    }
    
    private void HandleHostStartupStatusChanged(HostStartupStatus status)
    {
        hostStartupStatus = status;
        hasHostStartupStatus = true;

        AddLog($"HOST STAGE: {status.Stage}");
    }
    
    private void HandleJoinStartupStatusChanged(JoinStartupStatus status)
    {
        joinStartupStatus = status;
        hasJoinStartupStatus = true;

        AddLog($"JOIN STAGE: {status.Stage}");
    }

}


