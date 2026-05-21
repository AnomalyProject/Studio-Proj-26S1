using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; 
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

    [Header("Style & Functionality Settings")]
    [SerializeField] private CanvasGroup chatCanvasGroup;
    [SerializeField] private RectTransform chatContainerRect;
    [SerializeField] private float compactHeight = 150f;
    [SerializeField] private float expandedHeight = 400f;
    [SerializeField] private float fadeDelay = 4f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Autocomplete")]
    private string[] availableCommands = new string[] { "/mute", "/unmute", "/whisper", "/clear" };
    private List<string> autocompleteMatches = new List<string>();
    private int autocompleteIndex = -1;
    private bool isAutocompleting = false;
    private string originalPrefix = "";
    private bool isCompletingName = false;
    private string originalCommand = "";

    private float lastCloseTime = 0f;
    private float timeSinceLastMessage = 0f;
    private bool isChatOpen = false;

    private InputAction submit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(transform.root.gameObject);

        chatInputField.characterLimit = maxMessageLength;
        chatHistoryText.text = "";

        submit = InputBridge.Actions.Chat.Submit;


        CloseChat();

        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        InputBridge.OnContextChanged += OnToggleChatPerformed;
        chatInputField.onSubmit.AddListener(OnChatSubmit);
        chatInputField.onValueChanged.AddListener(OnChatValueChanged);

        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        InputBridge.OnContextChanged -= OnToggleChatPerformed;
        chatInputField.onSubmit.RemoveListener(OnChatSubmit);
        chatInputField.onValueChanged.RemoveListener(OnChatValueChanged);

        GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        if (!isChatOpen && chatCanvasGroup != null)
        {
            timeSinceLastMessage += Time.unscaledDeltaTime;

            if (timeSinceLastMessage >= fadeDelay)
            {
                chatCanvasGroup.alpha = Mathf.MoveTowards(chatCanvasGroup.alpha, 0f, Time.unscaledDeltaTime * fadeSpeed);
            }
        }
        else if (isChatOpen && chatInputField.isFocused)
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HandleAutocomplete();
            }
        }
    }

    private void HandleAutocomplete()
    {
        string currentText = chatInputField.text;

        if (string.IsNullOrWhiteSpace(currentText) || !currentText.StartsWith("/")) return;

        if (autocompleteMatches.Count == 0)
        {
            string[] parts = currentText.Split(' ');

            if (parts.Length == 1)
            {
                isCompletingName = false;
                originalPrefix = parts[0].ToLower();
                autocompleteMatches = availableCommands.Where(cmd => cmd.StartsWith(originalPrefix)).ToList();
            }
            else
            {
                isCompletingName = true;
                originalCommand = parts[0];
                originalPrefix = string.Join(" ", parts.Skip(1)).ToLower();

                IEnumerable<string> playerNames = Enumerable.Empty<string>();

                SessionData serverSession = SessionManager.Instance.CurrentSession;
                ClientSessionData clientSession = SessionManager.Instance.LatestClientSession;

                if (serverSession != null && serverSession.Players != null)
                {
                    playerNames = serverSession.Players.Select(p => p.DisplayName);
                }
                else if (clientSession.Players != null)
                {
                    playerNames = clientSession.Players.Select(p => p.DisplayName);
                }

                autocompleteMatches = playerNames.Where(name => name.ToLower().Contains(originalPrefix)).ToList();
            }
        }

        if (autocompleteMatches.Count > 0)
        {
            isAutocompleting = true;

            autocompleteIndex = (autocompleteIndex + 1) % autocompleteMatches.Count;
            string match = autocompleteMatches[autocompleteIndex];

            if (!isCompletingName)
            {
                chatInputField.text = match + " ";
            }
            else
            {
                chatInputField.text = $"{originalCommand} {match} ";
            }

            chatInputField.caretPosition = chatInputField.text.Length;
            StartCoroutine(FocusChatNextFrame());

            isAutocompleting = false;
        }
    }

    private void OnChatValueChanged(string text)
    {
        if (!isAutocompleting)
        {
            autocompleteMatches.Clear();
            autocompleteIndex = -1;
        }
    }
    private void OnToggleChatPerformed(InputBridge.InputContext context)
    {
        if (Time.unscaledTime - lastCloseTime < 0.1f) return;

        if (context == InputBridge.InputContext.Chat) OpenChat();
        else CloseChat();
    }

    private void OnChatSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (text.StartsWith("/"))
        {
            ProcessCommand(text);
        }
        else
        {
            ulong localSteamID = SteamUser.GetSteamID().m_SteamID;
            TextChatManager.Instance.SendChatMessage(text, localSteamID);
        }

        chatInputField.text = "";
        InputBridge.RestorePreviousContext();
        CloseChat();
    }

    private void ProcessCommand(string input)
    {
        string[] parts = input.Split(' ');
        string command = parts[0].ToLower();

        switch (command)
        {
            case "/mute":
                if (parts.Length > 1) MutePlayer(string.Join(" ", parts, 1, parts.Length - 1));
                break;
            case "/unmute":
                if (parts.Length > 1) UnmutePlayer(string.Join(" ", parts, 1, parts.Length - 1));
                break;
            case "/whisper":
                if (parts.Length > 2) WhisperToPlayer(parts[1], string.Join(" ", parts, 2, parts.Length - 2));
                break;
            case "/help":
                ReceiveMessage($"\"<color=yellow>[System]</color>\" ", "Available commands: /mute [player], /unmute [player], /whisper [player] [message]");
                break;
            default:
                ReceiveMessage($"\"<color=red>[System]</color>\" ", "Unknown command");
                break;
        }
    }

    private void OpenChat()
    {
        isChatOpen = true;
        timeSinceLastMessage = 0f;

        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 1f;
        }

        if (chatContainerRect != null)
        {
            chatContainerRect.sizeDelta = new Vector2(chatContainerRect.sizeDelta.x, expandedHeight);
        }

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
        isChatOpen = false;
        timeSinceLastMessage = 0f;

        if (chatContainerRect != null)
        {
            chatContainerRect.sizeDelta = new Vector2(chatContainerRect.sizeDelta.x, compactHeight);
        }

        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }

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
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 0f;
        }
        CloseChat();
    }

    public void ReceiveMessage(string displayName, string message)
    {
        string formattedMessage = $"<b>[{displayName}]:</b> {message}\n";
        chatHistoryText.text += formattedMessage;

        timeSinceLastMessage = 0f;

        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 1f;
        }

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

    private void MutePlayer(string playerName)
    {
        TextChatManager.Instance.SetMute(playerName, true);
        ReceiveMessage("<color=#FFD700>[System]</color>", $"Muted {playerName}.");
    }

    private void UnmutePlayer(string playerName)
    {
        TextChatManager.Instance.SetMute(playerName, false);
        ReceiveMessage("<color=#FFD700>[System]</color>", $"Unmuted {playerName}.");
    }

    private void WhisperToPlayer(string targetName, string message)
    {
        ulong localSteamID = SteamUser.GetSteamID().m_SteamID;
        TextChatManager.Instance.SendWhisper(targetName, message, localSteamID);

        //ReceiveMessage($"<color=purple>[Whisper to {targetName}]</color>", message);   <-- Debug
    }
}