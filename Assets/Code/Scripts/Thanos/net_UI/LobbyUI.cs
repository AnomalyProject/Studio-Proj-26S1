using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The main container for the lobby UI")]
    [SerializeField] private GameObject lobbyPanel;

    [Header("Player List")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private PlayerListUI playerListItemPrefab;
    [SerializeField] private TMP_Text playerCountText;

    [Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;

    private void Awake()
    {
        //Connecting UI interactions to SessionManager
        readyButton.onClick.AddListener(() => SessionManager.Instance.RequestToggleReady());
        startButton.onClick.AddListener(() => SessionManager.Instance.RequestStartMatch());
        leaveButton.onClick.AddListener(OnLeaveClicked);
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
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
        
        SessionEvents.OnSessionDataChanged -= RefreshUI;
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
        if (SessionManager.Instance == null || SessionManager.Instance.LatestClientSession.Players == null) 
            return;

        var sessionData = SessionManager.Instance.LatestClientSession;

        //Updating Player count display
        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {sessionData.PlayerCount}/{sessionData.MaxPlayers}";
        }

        //Clearing old list items
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        bool allPlayersReady = true;
        bool isLocalPlayerReady = false;
        ulong localSteamID = SteamUser.GetSteamID().m_SteamID;

        //Using new list items and calculating states
        foreach (var player in sessionData.Players)
        {
            var listItem = Instantiate(playerListItemPrefab, playerListContainer);
            listItem.Setup(player);

            if (!player.IsReady) 
            {
                allPlayersReady = false;
            }

            if (player.SteamID == localSteamID)
            {
                isLocalPlayerReady = player.IsReady;
            }
        }

        //Updating ready status
        if (readyButtonText != null)
        {
            readyButtonText.text = isLocalPlayerReady ? "Unready" : "Ready";
        }

        //Updating start button - only the host can see it, if everyone is ready he can press it-
        bool isHost = SessionManager.Instance.IsHost;
        startButton.gameObject.SetActive(isHost);
        startButton.interactable = isHost && allPlayersReady && sessionData.PlayerCount > 0;
    }

    private void OnLeaveClicked()
    {
        //We say bye bye to the server and leave
        SessionManager.Instance.RequestLeaveSession();
        
        //Maybe we can modify the SessionManager to handle this transition here better, but since it doesnt we do this here locally for now.
        GameStateManager.Instance.RequestStateChange(GameState.Menu);
        //TODO:After lead is done on the main scripts, revise this shi and refactor what is needed.
    }
}