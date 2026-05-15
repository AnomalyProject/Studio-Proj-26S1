using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;
using PurrNet.Modules;
using System.Collections.Generic;
using UnityEngine.UI;

public class MultiplayerSceneLoader : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup loadingOverlay;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float fadeDuration = 0.5f;

    private readonly Dictionary<GameState, string> _stateToScene = new Dictionary<GameState, string>
    {
        { GameState.Menu, "MainMenuScene" },
        { GameState.Lobby, "LobbyScene" },
        { GameState.InGame, "MainGameplayScene" }
    };

    private GameState _pendingTargetState;

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

    private void HandleStateChange(GameState prev, GameState next)
    {
        if (NetworkManager.main == null) return;

        if (next == GameState.Loading && NetworkManager.isServerStatic)
        {
            _pendingTargetState = (prev == GameState.Lobby) ? GameState.InGame : GameState.Lobby;

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

    private System.Collections.IEnumerator ServerLoadSequence(string sceneName)
    {
        StopAllCoroutines();
        progressBar.value = 0f;
        yield return StartCoroutine(Fade(1));

        PurrSceneSettings settings = new()
        {
            isPublic = true,
            mode = LoadSceneMode.Single,
        };

        NetworkManager.main.sceneModule.LoadSceneAsync(sceneName, settings);
    }

    private void OnPreSceneLoaded(SceneID scene, bool asServer)
    {
        if (asServer) return;

        StopAllCoroutines();
        progressBar.value = 0f;
        StartCoroutine(Fade(1));
    }

    private void OnPostSceneLoaded(SceneID scene, bool asServer)
    {
        if (asServer)
        {
            if (GameStateManager.Instance.CurrentState == GameState.Loading)
            {
                GameStateManager.Instance.RequestStateChange(_pendingTargetState);
            }
            return;
        }

        StopAllCoroutines();
        progressBar.value = 1f;
        StartCoroutine(Fade(0));
    }

    private System.Collections.IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = loadingOverlay.alpha;
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