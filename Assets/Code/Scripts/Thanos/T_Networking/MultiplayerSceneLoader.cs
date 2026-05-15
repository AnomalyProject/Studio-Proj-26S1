using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;
using PurrNet.Modules;
using System.Collections.Generic;
using UnityEngine.UI;

public class SceneTransitionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup loadingOverlay;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float fadeDuration = 0.5f;

    private readonly Dictionary<GameState, string> _stateToScene = new Dictionary<GameState, string>
    {
        { GameState.Menu, "MainMenuScene" },
        { GameState.Lobby, "LobbyScene" },
        { GameState.InGame, "MainGameplayScene" }
    };

    private void Start()
    {
        GameStateManager.Instance.OnStateChanged += HandleStateChange;

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
    }

    private void HandleStateChange(GameState prev, GameState next)
    {
        if (next == GameState.Loading)
        {
            GameState targetState = (prev == GameState.Lobby) ? GameState.InGame : GameState.Lobby;
            StartCoroutine(PerformTransition(targetState));
        }
        else if (prev == GameState.Loading)
        {
            StartCoroutine(Fade(0));
        }
    }

    private System.Collections.IEnumerator PerformTransition(GameState targetState)
    {
        progressBar.value = 0f;

        yield return StartCoroutine(Fade(1));

        float simulatedLoadTime = 2f;
        float timer = 0;
        while (timer < simulatedLoadTime)
        {
            timer += Time.deltaTime;
            progressBar.value = timer / simulatedLoadTime;
            yield return null;
        }

        if (NetworkManager.isServerStatic)
        {
            if (_stateToScene.TryGetValue(targetState, out string sceneName))
            {
                PurrSceneSettings settings = new()
                {
                    isPublic = true,
                    mode = LoadSceneMode.Single,
                };

                NetworkManager.main.sceneModule.LoadSceneAsync(sceneName, settings);

                GameStateManager.Instance.RequestStateChange(targetState);
            }
            else
            {
                Debug.LogError($"[SceneTransition]: No scene mapped for state {targetState}");
            }
        }
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