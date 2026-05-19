using UnityEngine;
using PurrNet;
using UnityEngine.InputSystem;

public class TextChatManager : NetworkBehaviour 
{
    public static TextChatManager Instance { get; private set; }

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

    private void HandlePlayerJoined(ulong steamID, string displayName)
    {
        if (isServer)
        {
            BroadcastSystemMessage($"Player <b>{displayName}</b> has joined the session.");
        }
    }

    private void HandlePlayerLeft(ulong steamID, string reason)
    {
        if(!isServer) return;

        string displayName = "John Anomaly";
        SessionData currentSession = SessionManager.Instance.CurrentSession;

        if(currentSession != null)
        {
            PlayerSessionInfo? playerInfo = currentSession.GetPlayer(steamID);
            if (playerInfo.HasValue)
            {
                displayName = playerInfo.Value.DisplayName;
            }
            else
            {
                Debug.LogWarning($"[ChatManager] Player left with unknown SteamID: {steamID}");
            }
        }

        if (isServer)
        {
            BroadcastSystemMessage($"Player <b>{displayName}</b> has left the session.");
        }
    }

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
    
    [ObserversRpc(bufferLast: true)] //Save last RPC message to be broadcasted to the next player that joins the room 
    private void BroadcastMessage(string displayName, string message)
    {
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
}