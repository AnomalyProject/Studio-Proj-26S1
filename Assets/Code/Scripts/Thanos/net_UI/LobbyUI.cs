using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The main container for the lobby UI")]
    [SerializeField] private GameObject lobbyPanel;

    [Header("Player List")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private PlayerListUI playerListItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;
    
    [Header("Lobby Info")]
    [SerializeField] private TMP_Text readyCountText;
    
    [Header("Invite")]
    [SerializeField] private Button inviteButton;
    
    [Header("Host Controls")]
    [SerializeField] private GameObject hostControlsRoot;
    [SerializeField] private TMP_Dropdown privacyDropdown;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;

    [Header("Messages")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    
    private Coroutine messageCoroutine;
    
    private void Awake()
    {
        //Connecting UI interactions to SessionManager
        readyButton.onClick.AddListener(() => SessionManager.Instance.RequestToggleReady());
        startButton.onClick.AddListener(() => SessionManager.Instance.RequestStartMatch());
        leaveButton.onClick.AddListener(OnLeaveClicked);
        
        inviteButton.onClick.AddListener(OnInviteClicked);
        privacyDropdown.onValueChanged.AddListener(OnPrivacyChanged);
        maxPlayersDropdown.onValueChanged.AddListener(OnMaxPlayersChanged);
    }
    

    private void Start()
    {
        //Setting initial visibility in case we load into dat scene
        UpdateLobbyVisibility(GameStateManager.Instance.CurrentState);
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += HandleStateChanged;
        }

        //Subscribing to SessionManager event
        SessionEvents.OnSessionDataChanged += RefreshUI; 
        
        SessionEvents.OnSessionError += HandleSessionError;
        SessionEvents.OnHostMigrationStarted += HandleHostMigrationStarted;

    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
        
        SessionEvents.OnSessionDataChanged -= RefreshUI;
        
        SessionEvents.OnSessionError -= HandleSessionError;
        SessionEvents.OnHostMigrationStarted -= HandleHostMigrationStarted;
    }

    private void HandleStateChanged(GameState previousState, GameState newState)
    {
        UpdateLobbyVisibility(newState);
    }

    private void UpdateLobbyVisibility(GameState state)
    {
        bool isLobby = state == GameState.Lobby;
        lobbyPanel.SetActive(isLobby);
        
        if (isLobby)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (SessionManager.Instance == null || SessionManager.Instance.LatestClientSession.Players == null) return;

        var sessionData = SessionManager.Instance.LatestClientSession;

        //Clearing old list items
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        bool allPlayersReady = true;
        bool isLocalPlayerReady = false;
        bool isLocalPlayerInElevator = false;
        int readyCount = 0;
        ulong localSteamID = SteamUser.GetSteamID().m_SteamID;

        //Using new list items and calculating states
        foreach (var player in sessionData.Players)
        {
            var listItem = Instantiate(playerListItemPrefab, playerListContainer);
            listItem.Setup(player);

            if (player.IsReady && player.IsInElevator)
            {
                readyCount++;
            }
            else
            {
                allPlayersReady = false;
            }

            if (player.SteamID == localSteamID)
            {
                isLocalPlayerReady = player.IsReady;
                isLocalPlayerInElevator = player.IsInElevator;
            }
        }
        
        readyCountText.text = $"{readyCount}/{sessionData.PlayerCount} Ready In Elevator";

        //Updating ready status
        if (readyButton != null)
        {
            readyButton.interactable =
                isLocalPlayerInElevator &&
                sessionData.ElevatorState == ElevatorLobbyState.Open;
        }

        if (readyButtonText != null)
        {
            if (sessionData.ElevatorState == ElevatorLobbyState.DoorsClosing)
            {
                readyButtonText.text = "Doors Closing";
            }
            else if (sessionData.ElevatorState == ElevatorLobbyState.DoorsClosed)
            {
                readyButtonText.text = "Leaving";
            }
            else
            {
                readyButtonText.text = isLocalPlayerReady ? "Unready" : "Ready";
            }
        }

        //Updating start button - only the host can see it, if everyone is ready he can press it-
        bool isHost = SessionManager.Instance.IsHost;
        
        hostControlsRoot.SetActive(isHost);
        inviteButton.interactable = SteamSessionBridge.Instance != null;
        
        ApplyLobbySettings(sessionData, isHost);
        
        startButton.gameObject.SetActive(isHost);
        startButton.interactable = isHost && allPlayersReady && sessionData.PlayerCount > 0;
    }
    
    private void OnPrivacyChanged(int index)
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.IsHost) return;

        string visibility = index == 1 ? "Public" : "Friends Only";

        if (SteamSessionBridge.Instance == null || !SteamSessionBridge.Instance.TrySetLobbyVisibility(visibility))
        {
            ShowMessage("Failed to update lobby visibility.");
            RefreshUI();
            return;
        }

        SessionManager.Instance.RequestUpdateSettings("LobbyVisibility", visibility);
    }
    
    private void OnInviteClicked()
    {
        if (SteamSessionBridge.Instance == null || !SteamSessionBridge.Instance.TryOpenInviteOverlay())
        {
            ShowMessage("Could not open the Steam invite overlay.");
        }
    }
    
    private void OnMaxPlayersChanged(int index)
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.IsHost) return;

        var sessionData = SessionManager.Instance.LatestClientSession;
        int maxPlayers = index + 2;

        if (maxPlayers < sessionData.PlayerCount)
        {
            ShowMessage("Max players cannot be lower than the current player count.");
            maxPlayersDropdown.SetValueWithoutNotify(Mathf.Clamp(sessionData.MaxPlayers, 2, 4) - 2);
            return;
        }

        if (SteamSessionBridge.Instance == null || !SteamSessionBridge.Instance.TrySetLobbyMaxPlayers(maxPlayers))
        {
            ShowMessage("Failed to update max players.");
            maxPlayersDropdown.SetValueWithoutNotify(Mathf.Clamp(sessionData.MaxPlayers, 2, 4) - 2);
            return;
        }

        SessionManager.Instance.RequestUpdateSettings("MaxPlayers", maxPlayers.ToString());
    }

    private void HandleSessionError(SessionErrorResponse error)
    {
        ShowMessage(error.Message, 3f);
    }
    
    private void HandleHostMigrationStarted(string newHostName)
    {
        ShowMessage("Host left the lobby.", 2f);
    }

    private void OnLeaveClicked()
    {
        SessionModeManager.Instance.ReturnToMenu();
    }
    
    private void ApplyLobbySettings(ClientSessionData sessionData, bool isHost)
    {
        if (privacyDropdown != null)
        {
            string visibility = GetCustomProperty(sessionData, "LobbyVisibility");
            if (string.IsNullOrEmpty(visibility))
            {
                visibility = "Friends Only";
            }

            privacyDropdown.SetValueWithoutNotify(visibility == "Public" ? 1 : 0);
            privacyDropdown.interactable = isHost;
        }

        if (maxPlayersDropdown != null)
        {
            int clampedMaxPlayers = Mathf.Clamp(sessionData.MaxPlayers, 2, 4);
            maxPlayersDropdown.SetValueWithoutNotify(clampedMaxPlayers - 2);
            maxPlayersDropdown.interactable = isHost;
        }
    }
    
    private string GetCustomProperty(ClientSessionData sessionData, string key)
    {
        if (sessionData.CustomPropertyKeys == null || sessionData.CustomPropertyValues == null)
            return null;

        int count = Mathf.Min(sessionData.CustomPropertyKeys.Count, sessionData.CustomPropertyValues.Count);

        for (int i = 0; i < count; i++)
        {
            if (sessionData.CustomPropertyKeys[i] == key)
            {
                return sessionData.CustomPropertyValues[i];
            }
        }

        return null;
    }
    
    private void ShowMessage(string message, float duration = 2.5f)
    {
        if (messagePanel == null || messageText == null)
            return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine = StartCoroutine(ShowMessageRoutine(message, duration));
    }
    
    private IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messagePanel.SetActive(true);
        messageText.text = message;

        yield return new WaitForSecondsRealtime(duration);

        messagePanel.SetActive(false);
        messageCoroutine = null;
    }
}