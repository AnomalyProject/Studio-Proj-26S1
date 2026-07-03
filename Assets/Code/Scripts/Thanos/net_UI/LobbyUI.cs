using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The main container for the lobby UI")]
    [SerializeField] private GameObject lobbyPanel;

    [Header("Player List")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private PlayerListUI playerListItemPrefab;

    /*[Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;*/
    
    [Header("Lobby Info")]
    [SerializeField] private TMP_Text readyCountText;
    
    [Header("Multiplayer")]
    [SerializeField] private GameObject multiplayerSettings;

    [Header("Host Controls")]
    [SerializeField] private GameObject hostControlsRoot;
    /*[SerializeField] private GameObject leftControlsRoot;
    [SerializeField] private TMP_Dropdown privacyDropdown;*/
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;

    [Header("Messages")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    
    [Header("Kick to Vote")]
    [SerializeField] private GameObject kickReasonPanel;
    [SerializeField] private TMP_Text kickReasonTargetText;
    [SerializeField] private TMP_Dropdown kickReasonDropdown;
    [SerializeField] private Button kickReasonConfirmButton;
    [SerializeField] private Button kickReasonCancelButton;
    
    private Coroutine messageCoroutine;
    private ulong pendingKickTargetSteamID;
    
    private void Awake()
    {
        //Connecting UI interactions to SessionManager
        /*readyButton.onClick.AddListener(() => SessionManager.Instance.RequestToggleReady());
        startButton.onClick.AddListener(() => SessionManager.Instance.RequestStartMatch());
        leaveButton.onClick.AddListener(OnLeaveClicked);*/
        
        //privacyDropdown.onValueChanged.AddListener(OnPrivacyChanged);
        maxPlayersDropdown.onValueChanged.AddListener(OnMaxPlayersChanged);
        
        kickReasonConfirmButton.onClick.AddListener(OnKickReasonConfirmed);
        kickReasonCancelButton.onClick.AddListener(HideKickReasonPanel);
        
        SetupKickReasonDropdown();
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
        
        SessionEvents.OnKickVoteFinished += HandleKickVoteFinished;

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
        
        SessionEvents.OnKickVoteFinished -= HandleKickVoteFinished;
    }

    private void HandleStateChanged(GameState previousState, GameState newState)
    {
        UpdateLobbyVisibility(newState);
    }

    private void UpdateLobbyVisibility(GameState state)
    {
        bool shouldShowLobbyUI = (state == GameState.Lobby || state == GameState.InGame) && !IsSoloMode();

        lobbyPanel.SetActive(shouldShowLobbyUI);

        if (shouldShowLobbyUI)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (IsSoloMode())
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            return;
        }
        
        if (SessionManager.Instance == null || SessionManager.Instance.LatestClientSession.Players == null) return;

        var sessionData = SessionManager.Instance.LatestClientSession;

        //Clearing old list items
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }
        
        //Updating start button - only the host can see it, if everyone is ready he can press it-
        bool isHost = SessionManager.Instance.IsHost;
        bool isLobby = GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Lobby;
        bool isInGame = GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.InGame;
        bool canStartKickVote = sessionData.Players != null && sessionData.Players.Count(player => player.IsConnected) >= 3 && (isInGame || (isLobby && sessionData.ElevatorState == ElevatorLobbyState.Open));


        bool allPlayersReady = true;
        bool isLocalPlayerReady = false;
        bool isLocalPlayerInElevator = false;
        int readyCount = 0;
        ulong localSteamID = 0;

        if (SessionModeManager.Instance != null) SessionModeManager.Instance.TryGetLocalSessionSteamID(out localSteamID);
        
       
        //Using new list items and calculating states
        foreach (var player in sessionData.Players)
        {
            var listItem = Instantiate(playerListItemPrefab, playerListContainer);
            listItem.Setup(player, localSteamID, OpenKickReasonPanel, canStartKickVote);

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
        /*if (readyButton != null)
        {
            readyButton.gameObject.SetActive(isLobby);
            
            readyButton.interactable = isLocalPlayerInElevator && !isLocalPlayerReady && sessionData.ElevatorState == ElevatorLobbyState.Open;
        }*/

        /*if (readyButtonText != null)
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
                if (isLocalPlayerReady && isLocalPlayerInElevator)
                {
                    readyButtonText.text = "Ready";
                }
                else if (!isLocalPlayerInElevator)
                {
                    readyButtonText.text = "Enter Elevator";
                }
                else
                {
                    readyButtonText.text = "Ready";
                }
            }
        }*/

        //hostControlsRoot.SetActive(isLobby && isHost);
        //leftControlsRoot.SetActive(isLobby);
        
        multiplayerSettings.SetActive(isHost && isLobby && SteamSessionBridge.Instance != null);

        ApplyLobbySettings(sessionData, isHost);
        
        /*startButton.gameObject.SetActive(false);
        startButton.interactable = false;*/
    }
    
    private void OpenKickReasonPanel(ulong targetSteamID)
    {
        pendingKickTargetSteamID = targetSteamID;
        
        if (kickReasonPanel) kickReasonPanel.SetActive(true);
        
        if (kickReasonTargetText) kickReasonTargetText.text = $"Remove {GetPlayerDisplayName(targetSteamID)}?";
    }
    
    private void SetupKickReasonDropdown()
    {
        if (kickReasonDropdown == null) return;

        kickReasonDropdown.ClearOptions();

        List<string> options = new List<string>
        {
            SessionKickReason.AfkNotParticipating.ToDisplayText(),
            SessionKickReason.PreventingGroupFromContinuing.ToDisplayText(),
            SessionKickReason.HarassmentAbusiveCommunication.ToDisplayText(),
            SessionKickReason.CheatingOrExploiting.ToDisplayText(),
            SessionKickReason.Other.ToDisplayText()
        };

        kickReasonDropdown.AddOptions(options);
        kickReasonDropdown.SetValueWithoutNotify(0);
    }

    private void OnKickReasonConfirmed()
    {
        ConfirmKickVote(kickReasonDropdown.value);
    }

    private void HideKickReasonPanel()
    {
        if (kickReasonPanel != null) kickReasonPanel.SetActive(false);

        pendingKickTargetSteamID = 0;
    }

    private string GetPlayerDisplayName(ulong steamID)
    {
        var sessionData = SessionManager.Instance.LatestClientSession;

        if (sessionData.Players == null) return "player";

        foreach (var player in sessionData.Players)
        {
            if (player.SteamID == steamID) return player.DisplayName;
        }

        return "player";
    }

    public void ConfirmKickVote(int reasonIndex)
    {
        if (pendingKickTargetSteamID == 0) return;
        
        SessionKickReason reason = (SessionKickReason)reasonIndex;
        SessionManager.Instance.RequestStartKickVote(pendingKickTargetSteamID, reason);
        
        HideKickReasonPanel();
        
    }
    
    private void OnPrivacyChanged(int index)
    {
        if (IsSoloMode()) return;
        
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
    
    public void OnInviteClicked()
    {
        if (IsSoloMode()) return;
        if (SteamSessionBridge.Instance == null || !SteamSessionBridge.Instance.TryOpenInviteOverlay())
        {
            ShowMessage("Could not open the Steam invite overlay.");
        }
    }
    
    public void OnMaxPlayersChanged(int index)
    {
        if (IsSoloMode()) return;
        if (SessionManager.Instance == null || !SessionManager.Instance.IsHost) return;

        var sessionData = SessionManager.Instance.LatestClientSession;
        int maxPlayers = index + 2;

        if (maxPlayers < sessionData.PlayerCount)
        {
            ShowMessage("Max players cannot be lower than the current player count.");
            maxPlayersDropdown.SetValueWithoutNotify(Mathf.Clamp(sessionData.MaxPlayers, 2, 4) - 2);
            return;
        }

        // fix for dev testing


        bool isDevHost = SessionModeManager.Instance != null && SessionModeManager.Instance.CurrentMode == SessionMode.DevHost;

        if (!isDevHost && (SteamSessionBridge.Instance == null || !SteamSessionBridge.Instance.TrySetLobbyMaxPlayers(maxPlayers)))
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
    
    private void HandleKickVoteFinished(bool succeeded, string message)
    {
        if (string.IsNullOrWhiteSpace(message))  message = succeeded ? "Kick vote passed." : "Kick vote failed.";
        
        ShowMessage(message, succeeded ? 3f : 2.5f);
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
    {/*
        if (privacyDropdown != null)
        {
            string visibility = GetCustomProperty(sessionData, "LobbyVisibility");
            if (string.IsNullOrEmpty(visibility))
            {
                visibility = "Friends Only";
            }

            /*privacyDropdown.SetValueWithoutNotify(visibility == "Public" ? 1 : 0);
            privacyDropdown.interactable = isHost;#1#
        }*/

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
    
    private bool IsSoloMode()
    {
        return SessionModeManager.Instance != null &&
               SessionModeManager.Instance.CurrentMode == SessionMode.Solo;
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