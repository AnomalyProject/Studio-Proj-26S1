using UnityEngine;
using Steamworks;

public class SteamSessionBridge : MonoBehaviour
{
    public static SteamSessionBridge Instance { get; private set; }

    private CSteamID currentLobbyId;
    private bool isInLobby = false;

    // steam callbacks (always listening)
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    private Callback<GameLobbyJoinRequested_t> joinRequestedCallback;

    // steam callresults (one-shot for specific API calls)
    private CallResult<LobbyCreated_t> lobbyCreatedCallResult;

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

    private void Start()
    {
        // safety check
        if (!SteamManager.Initialized) return;

        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        joinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

        lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
    }
    
    private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure) { }
    private void OnLobbyEntered(LobbyEnter_t callback) { }
    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback) { }
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback) { }
}
