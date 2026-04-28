using System;
using UnityEngine;
using TMPro; 
using Steamworks;
using UnityEngine.UI;

public class ChatUI : MonoBehaviour
{
    public static ChatUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI chatHistoryText;
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Dependencies")]
    [SerializeField] private int maxMessageLength = 200;

    private void Awake()
    {
        Instance = this;
        chatInputField.characterLimit = maxMessageLength; 
        chatHistoryText.text = ""; 
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!chatInputField.isFocused)
            {
                chatInputField.ActivateInputField();
            }
        }
    }

    private void OnEnable()
    {
        chatInputField.onSubmit.AddListener(OnChatSubmit);
    }

    private void OnDisable()
    {
        chatInputField.onSubmit.RemoveListener(OnChatSubmit);
    }
    
    private void OnChatSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            chatInputField.text = "";
            chatInputField.DeactivateInputField();
            return;
        }

        ulong localSteamID = SteamUser.GetSteamID().m_SteamID;
        
        TextChatManager.Instance.SendChatMessageServerRpc(text, localSteamID);
        
        chatInputField.text = "";
        
        chatInputField.ActivateInputField(); 
    }
    public void ReceiveMessage(string displayName, string message)
    {
        string formattedMessage = $"<b>[{displayName}]:</b> {message}\n";
        chatHistoryText.text += formattedMessage;
        
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
    
    public bool IsTyping => chatInputField.isFocused; //Bool that we can use to stop other actions when the player is typing ex: if(ChatUI.Instance.IsTyping) return;
}