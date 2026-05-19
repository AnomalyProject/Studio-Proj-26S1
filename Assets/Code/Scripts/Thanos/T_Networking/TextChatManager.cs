using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System;

public class TextChatManager : NetworkBehaviour 
{
    public static TextChatManager Instance { get; private set; }

    public static Dictionary<ulong, string> PlayerNames = new Dictionary<ulong, string>();

    private HashSet<string> mutedPlayers = new HashSet<string>();
    private bool muteAll = false;

    RPCInfo info = default;

    [Header("Chat Settings")]
    [SerializeField] private int maxMessageLength = 200;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SessionEvents.OnPlayerJoined += HandlePlayerJoined;
        SessionEvents.OnPlayerLeft += HandlePlayerLeft;
    }

    private void OnDisable()
    {
        SessionEvents.OnPlayerJoined -= HandlePlayerJoined;
        SessionEvents.OnPlayerLeft -= HandlePlayerLeft;
    }

    /// <summary>
    /// Handles the event when a player joins the session by broadcasting a system message to all connected users.
    /// </summary>
    /// <param name="steamID">The unique Steam identifier of the player who joined.</param>
    /// <param name="displayName">The display name of the player who joined the session.</param>
    private void HandlePlayerJoined(ulong steamID, string displayName)
    {
        if(!PlayerNames.ContainsKey(steamID))
        {
            PlayerNames.Add(steamID, displayName);
        }
        else
        {
            PlayerNames[steamID] = displayName;
        }

        if(!isServer) return;

        BroadcastSystemMessage($"Player <b>{displayName}</b> has joined the session.");
    }

    /// <summary>
    /// Handles the event when a player leaves the session and broadcasts a system message to all connected users.
    /// </summary>
    /// <remarks>This method should be called only on the server. If the player's display name cannot be
    /// determined, a default name is used and a warning is logged.</remarks>
    /// <param name="steamID">The unique Steam identifier of the player who has left the session.</param>
    /// <param name="reason">The reason for the player's departure, as provided by the session or system.</param>
    private void HandlePlayerLeft(ulong steamID, string reason)
    {
        if(!isServer) return;

        string displayName = "John Anomaly";

        if(PlayerNames.TryGetValue(steamID, out string name))
        {
            displayName = name;
            PlayerNames.Remove(steamID);
        }

        SessionData currentSession = SessionManager.Instance.CurrentSession;

        if (isServer)
        {
            BroadcastSystemMessage($"Player <b>{displayName}</b> has left the session.");
        }
    }

    /// <summary>
    /// Sends a chat message from the specified sender to all connected clients on the server.
    /// </summary>
    /// <remarks>If the sender's SteamID does not correspond to a known player in the current session, the
    /// message is not sent and a warning is logged. This method can be called by any client, regardless of
    /// ownership.</remarks>
    /// <param name="message">The text of the chat message to send. Leading and trailing whitespace is ignored. If the message exceeds the
    /// maximum allowed length, it is truncated.</param>
    /// <param name="senderSteamID">The SteamID of the player sending the message. Used to identify the sender and display their name and color in
    /// the chat.</param>

    [ServerRpc (requireOwnership: false)] 
    public void SendChatMessage(string message, ulong senderSteamID)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        
        if (message.Length > maxMessageLength)
        {
            message = message.Substring(0, maxMessageLength); 
        }

        string displayName = "John Anomaly";
        
        SessionData currentSession = SessionManager.Instance.CurrentSession; 
        
        if (currentSession != null)
        {
            PlayerSessionInfo? playerInfo = currentSession.GetPlayer(senderSteamID);
            if (playerInfo.HasValue)
            {
                string name = playerInfo.Value.DisplayName;
                string hexColor = PlayerColour.GetHex(playerInfo.Value.ColorIndex);
                displayName = $"<color={hexColor}>{name}</color>";
            }
            else
            {
                Debug.LogWarning($"[ChatManager] Received message from unknown SteamID: {senderSteamID}");
                return;
            }
        }
        
        BroadcastMessage(displayName, message);
    }
    
    [ObserversRpc(bufferLast: false)] //Save last RPC message to be broadcasted to the next player that joins the room //TODO: MAKE BUFFER TRUE FOR CLIENT SYSTEM NOTIFS
    private void BroadcastMessage(string displayName, string message)
    {
        if (muteAll) return;
        string cleanName = System.Text.RegularExpressions.Regex.Replace(displayName, "<.*?>", "").ToLower(); //Remove rich text tags for mute checks

        if (mutedPlayers.Contains(cleanName)) return;

        if (ChatUI.Instance != null)
        {
            ChatUI.Instance.ReceiveMessage(displayName, message);
        }
    }

    private void BroadcastSystemMessage(string message)
    {
        if(!isServer) return; //Only the server can send system messages

        string systemName = "<color=#FFD700>System</color>";

        BroadcastMessage(systemName, message);
    }

    public void SetMute(string playerName, bool muted)
    {
        if (playerName.ToLower() == "all")
        {
            muteAll = muted;
        }
        else
        {
            if (muted) mutedPlayers.Add(playerName);
            else mutedPlayers.Remove(playerName);
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void SendWhisper(string targetName, string message, ulong senderSteamID)
    {
        ulong targetSteamID = 0;

        SessionData currsession = SessionManager.Instance.CurrentSession;

        if(currsession != null)
        {
            foreach(var player in currsession.Players)
            {
                if (player.DisplayName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    targetSteamID = player.SteamID;
                    break;
                }
            }
        }

        if(targetSteamID == 0)
        {
            SendWhisperError(info.sender, $"Player '{targetName}' not found.");
            return;
        }

        PlayerID? targetNetworkID = SessionManager.Instance.GetPlayerIDForSteam(targetSteamID);
        if(targetSteamID == 0)
        {
            SendWhisperToClient(targetNetworkID.Value, message, senderSteamID);
        }
    }

    [TargetRpc]
    private void SendWhisperToClient(PlayerID target, string message, ulong senderSteamID)
    {
        string senderName = "Unknown";
        SessionData session = SessionManager.Instance.CurrentSession;
        var senderInfo = session.GetPlayer(senderSteamID);
        if(senderInfo.HasValue) senderName = senderInfo.Value.DisplayName;

        if(ChatUI.Instance != null)
        {
            ChatUI.Instance.ReceiveMessage($"<color=purple>[Whisper from {senderName}]</color>", message);
        }
    }

    [TargetRpc]
    private void SendWhisperError(PlayerID target, string errorMessage)
    {
        if(ChatUI.Instance != null)
        {
            ChatUI.Instance.ReceiveMessage($"<color=red>[System]</color>", errorMessage);
        }
    }
}