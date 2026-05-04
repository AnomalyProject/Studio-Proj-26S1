using Steamworks;
using System;
using System.Collections;
using TMPro; 
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
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
    
    [Header("UI Toggling")]
    [SerializeField] private Image scrollViewBackground;
    [SerializeField] private GameObject scrollbarObject;
    
    private float lastCloseTime = 0f;

    private InputAction submit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // DontDestroyOnLoad needs to be on the parent object 
        DontDestroyOnLoad(transform.root.gameObject);
        
        chatInputField.characterLimit = maxMessageLength; 
        chatHistoryText.text = "";

        submit = InputBridge.Actions.Chat.Submit;


        CloseChat();
    }

    private void OnEnable()
    {
        InputBridge.OnContextChanged += OnToggleChatPerformed;
        chatInputField.onSubmit.AddListener(OnChatSubmit);

        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
    }
    
    private void OnDisable()
    {
        InputBridge.OnContextChanged -= OnToggleChatPerformed;
        chatInputField.onSubmit.RemoveListener(OnChatSubmit);
        
        GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
    }
    
    private void OnToggleChatPerformed(InputBridge.InputContext context)
    {
        if (Time.unscaledTime - lastCloseTime < 0.1f) return;
        
        if (context == InputBridge.InputContext.Chat) OpenChat();
        else CloseChat();
    }
    
    private void OnChatSubmit(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            ulong localSteamID = SteamUser.GetSteamID().m_SteamID;
            TextChatManager.Instance.SendChatMessage(text, localSteamID);
        }
        
        chatInputField.text = "";
        InputBridge.RestorePreviousContext();
        CloseChat();
    }
    
    private void OpenChat()
    {
        chatInputField.gameObject.SetActive(true);
        if (scrollViewBackground != null) scrollViewBackground.enabled = true;
        if (scrollbarObject != null) scrollbarObject.SetActive(true);
        
        ToggleGameplayInputs(true);
        
        StartCoroutine(FocusChatNextFrame());
    }
    private IEnumerator FocusChatNextFrame()
    {
        yield return null; 
        chatInputField.ActivateInputField();
        chatInputField.Select();
    }
    
    private void CloseChat()
    {
        chatInputField.gameObject.SetActive(false);
        if (scrollViewBackground != null) scrollViewBackground.enabled = false;
        if (scrollbarObject != null) scrollbarObject.SetActive(false);
        
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        
        ToggleGameplayInputs(false);
        
        lastCloseTime = Time.unscaledTime;
    }

    private void ClearChat()
    {
        chatHistoryText.text = "";
        CloseChat();
    }
    
    public void ReceiveMessage(string displayName, string message)
    {
        string formattedMessage = $"<b>[{displayName}]:</b> {message}\n";
        chatHistoryText.text += formattedMessage;
        
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) 
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    public bool IsTyping => chatInputField.isFocused; //Bool that we can use to stop other actions when the player is typing ex: if(ChatUI.Instance.IsTyping) return;
    
    //Remove this ugly shi when we refactor inputs like human beings
    private void ToggleGameplayInputs(bool chatIsOpen)
    {
        PlayerInput[] playerControls = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (PlayerInput playerInput in playerControls)
        {
            playerInput.enabled = !chatIsOpen;
        }
    }
    
    private void OnGameStateChanged(GameState previous, GameState next)
    {
        if (next == GameState.Menu) ClearChat();
    }

}