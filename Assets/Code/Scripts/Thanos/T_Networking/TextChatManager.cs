using UnityEngine;
using PurrNet;
using UnityEngine.InputSystem;

public class TextChatManager : NetworkBehaviour 
{
    public static TextChatManager Instance { get; private set; }

    [Header("Chat Settings")]
    [SerializeField] private int maxMessageLength = 200;

    [SerializeField] private InputActionAsset playerMap;
    [SerializeField] private InputActionAsset consoleMap;
    [SerializeField] private InputActionAsset UIMap;



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
    public void SendChatMessageServerRpc(string message, ulong senderSteamID)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        
        if (message.Length > maxMessageLength)
        {
            message = message.Substring(0, maxMessageLength); 
        }

        string displayName = "Unknown Player";
        
        SessionData currentSession = SessionManager.Instance.CurrentSession; 
        
        if (currentSession != null)
        {
            PlayerSessionInfo? playerInfo = currentSession.GetPlayer(senderSteamID);
            if (playerInfo.HasValue)
            {
                displayName = playerInfo.Value.DisplayName;
            }
            else
            {
                Debug.LogWarning($"[ChatManager] Received message from unknown SteamID: {senderSteamID}");
                return;
            }
        }
        
        BroadcastMessage(displayName, message);
    }
    
    [ObserversRpc] 
    private void BroadcastMessage(string displayName, string message)
    {
        if (ChatUI.Instance != null)
        {
            ChatUI.Instance.ReceiveMessage(displayName, message);
        }
    }

    public void SetInputState(bool isChatting)
    {
        if (isChatting)
        {
            playerMap.Disable();
            consoleMap.Disable();
            UIMap.Enable();
        
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            UIMap.Disable();
            consoleMap.Disable();
            playerMap.Enable();
        
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}