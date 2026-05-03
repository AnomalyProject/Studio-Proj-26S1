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
    public void SendChatMessage(string message, RPCInfo info = default)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        
        if (message.Length > maxMessageLength)
        {
            message = message.Substring(0, maxMessageLength); 
        }
        
        if (SessionManager.Instance == null || !SessionManager.Instance.TryGetPlayerForConnection(info.sender, out PlayerSessionInfo playerInfo))
        {
            Debug.LogWarning($"[ChatManager] Message rejected from unknown player: {info.sender}");
            return;
        }
        
        string hexColor = PlayerColour.GetHex(playerInfo.ColorIndex);
        string displayName = $"<color={hexColor}>{playerInfo.DisplayName}</color>";

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

    /* //Testing input handling with component
    public void SetInputState(PlayerInput input)
    {
        input = GetComponent<PlayerInput>();
    }
    */
}