using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;
using PurrNet.Modules;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Coordinates scene transitions and loading screen management for multiplayer game state changes in a networked
/// environment.
/// </summary>
/// <remarks>The MultiplayerSceneLoader listens for game state changes and manages the asynchronous loading of
/// scenes across server and client instances. It ensures that all connected players are synchronized during
/// transitions, displaying loading overlays and tracking player readiness before advancing the game state. This
/// component is intended for use with networked multiplayer games where consistent scene transitions and player
/// coordination are required.</remarks>
public class MultiplayerSceneLoader : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup loadingOverlay;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float fadeDuration = 0.5f;

    /// <summary>
    /// Provides a mapping between game states from GameStateManager and their corresponding scene names.
    /// </summary>
    /// <remarks>This dictionary is used to determine which scene to load based on the current game state.
    /// Each key represents a specific game state, and the associated value is the name of the scene to be loaded for
    /// that state.</remarks>
    private readonly Dictionary<GameState, string> _stateToScene = new Dictionary<GameState, string>
    {
        { GameState.Menu, "MainMenuScene" },
        { GameState.Lobby, "LobbyScene" },
        { GameState.InGame, "MainGameplayScene" }
    };

    private GameState _pendingTargetState;
    private int _playersReady = 0;

    private void Start()
    {
        GameStateManager.Instance.OnStateChanged += HandleStateChange;

        if (NetworkManager.main != null && NetworkManager.main.sceneModule != null)
        {
            NetworkManager.main.sceneModule.onPreSceneLoaded += OnPreSceneLoaded;
            NetworkManager.main.sceneModule.onPostSceneLoaded += OnPostSceneLoaded;
        }

        loadingOverlay.alpha = 0;
        loadingOverlay.blocksRaycasts = false;
        progressBar.value = 0f;
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChange;
        }

        if (NetworkManager.main != null && NetworkManager.main.sceneModule != null)
        {
            NetworkManager.main.sceneModule.onPreSceneLoaded -= OnPreSceneLoaded;
            NetworkManager.main.sceneModule.onPostSceneLoaded -= OnPostSceneLoaded;
        }
    }

    /// <summary>
    /// Handles transitions between game states and initiates scene loading on the server when appropriate.
    /// </summary>
    /// <remarks>This method is intended to be called when the game state changes. When transitioning to the
    /// Loading state on the server, it determines the target state and initiates the corresponding scene load sequence.
    /// If no scene is mapped for the target state, an error is logged. This method has no effect if the network manager
    /// is not initialized.</remarks>
    /// <param name="prev">The previous game state before the transition.</param>
    /// <param name="next">The next game state after the transition.</param>
    private void HandleStateChange(GameState prev, GameState next)
    {
        if (NetworkManager.main == null) return;

        if (next == GameState.Loading && NetworkManager.isServerStatic)
        {
            _pendingTargetState = (prev == GameState.Lobby) ? GameState.InGame : GameState.Lobby;
            _playersReady = 0;

            if (_stateToScene.TryGetValue(_pendingTargetState, out string sceneName))
            {
                StartCoroutine(ServerLoadSequence(sceneName));
            }
            else
            {
                Debug.LogError($"[SceneCoordinator]: No scene mapped for state {_pendingTargetState}");
            }
        }
    }

    /// <summary>
    /// Performs the server-side sequence for loading a new scene asynchronously, including displaying a loading screen
    /// and initiating the scene transition.
    /// </summary>
    /// <remarks>This method is designed to be used as a coroutine on the server. It ensures that a loading
    /// screen is shown before starting the asynchronous scene load. The scene is loaded in single mode and is made
    /// public to connected clients.</remarks>
    /// <param name="sceneName">The name of the scene to load. Must correspond to a valid scene available to the server.</param>
    /// <returns>An enumerator that controls the sequence of loading operations. Intended for use with Unity's coroutine system.</returns>
    private IEnumerator ServerLoadSequence(string sceneName)
    {
        RpcShowLoadingScreen();

        yield return new WaitForSeconds(fadeDuration + 0.1f);

        PurrSceneSettings settings = new()
        {
            isPublic = true,
            mode = LoadSceneMode.Single,
        };

        NetworkManager.main.sceneModule.LoadSceneAsync(sceneName, settings);
    }

    /// <summary>
    /// Displays the loading screen to all connected observers by resetting and showing the progress bar.
    /// </summary>
    /// <remarks>This method is intended to be called remotely on all clients observing the object. It resets
    /// the progress bar and initiates the loading screen transition.</remarks>
    [ObserversRpc]
    private void RpcShowLoadingScreen()
    {
        StopAllCoroutines();
        progressBar.value = 0f;
        StartCoroutine(Fade(1));
    }

    /// <summary>
    /// Handles pre-loading logic for a scene, updating the loading overlay as needed based on the server context.
    /// </summary>
    /// <remarks>If the scene is loaded as a client and the loading overlay is not fully visible, this method
    /// ensures the overlay is displayed and blocks user interaction until loading completes.</remarks>
    /// <param name="scene">The identifier of the scene that is about to be loaded.</param>
    /// <param name="asServer">Indicates whether the scene is being loaded in server mode. Set to <see langword="true"/> if loading as a
    /// server; otherwise, <see langword="false"/>.</param>
    private void OnPreSceneLoaded(SceneID scene, bool asServer)
    {
        if (!asServer && loadingOverlay.alpha < 1)
        {
            loadingOverlay.alpha = 1;
            loadingOverlay.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Handles post-processing logic after a scene has been loaded, performing different actions depending on whether
    /// the context is server or client.
    /// </summary>
    /// <remarks>When called on the server, this method may trigger a game state transition if the current
    /// state is loading. On the client, it finalizes loading progress and signals readiness. This method is intended to
    /// be invoked after a scene load completes.</remarks>
    /// <param name="scene">The identifier of the scene that was loaded.</param>
    /// <param name="asServer">Indicates whether the method is being executed in a server context. Set to <see langword="true"/> for
    /// server-side processing; otherwise, <see langword="false"/> for client-side processing.</param>
    private void OnPostSceneLoaded(SceneID scene, bool asServer)
    {
        if (asServer)
        {
            return;
        }

        StopAllCoroutines();
        progressBar.value = 1f;
        
        CmdPlayerReady();
    }

    /// <summary>
    /// Signals to the server that the local player is ready to proceed after loading.
    /// </summary>
    /// <remarks>This method should be called by each client when they have finished loading and are ready to
    /// continue. The server tracks the number of ready players and will advance the game state once all connected
    /// clients have indicated readiness. This method can be invoked by any client, regardless of object
    /// ownership.</remarks>
    [ServerRpc(requireOwnership: false)]
    private void CmdPlayerReady()
    {
        _playersReady++;

        if (_playersReady >= NetworkManager.main.playerCount)
        {
            RpcDropLoadingScreen(_pendingTargetState);

            if(GameStateManager.Instance.CurrentState == GameState.Loading)
            {
                GameStateManager.Instance.RequestStateChange(_pendingTargetState);
            }
        }
    }

    /// <summary>
    /// Instructs all connected clients to remove the loading screen and optionally transition to the specified game
    /// state.
    /// </summary>
    /// <remarks>This method is called remotely on all observers. On non-server clients, it forces a state
    /// change to the specified target state after removing the loading screen.</remarks>
    /// <param name="targetState">The game state to transition to if the current instance is not the server.</param>
    [ObserversRpc]
    private void RpcDropLoadingScreen(GameState targetState)
    {
        StartCoroutine(Fade(0));

        if(!NetworkManager.isServerStatic)
        {
            GameStateManager.Instance.ForceStateChange(targetState);
        }
    }

    /// <summary>
    /// Gradually transitions the loading overlay's alpha value to the specified target alpha over the configured fade
    /// duration.
    /// </summary>
    /// <remarks>If the fade is interrupted and restarted before completion, the transition will begin from
    /// the current alpha value, ensuring a smooth effect. The loading overlay's ability to block raycasts is enabled if
    /// the target alpha is greater than 0, and disabled otherwise.</remarks>
    /// <param name="targetAlpha">The final alpha value to fade to. Valid values are between 0 (fully transparent) and 1 (fully opaque).</param>
    /// <returns>An enumerator that performs the fade operation over multiple frames. This can be used with a coroutine to
    /// animate the transition.</returns>
    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = loadingOverlay.alpha; // Capture the starting alpha to ensure smooth fading even if interrupted
        float timer = 0;
        loadingOverlay.blocksRaycasts = targetAlpha > 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            loadingOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        loadingOverlay.alpha = targetAlpha;
    }
}