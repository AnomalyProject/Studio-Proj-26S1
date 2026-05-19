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
}