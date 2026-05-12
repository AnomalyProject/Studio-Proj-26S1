using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using PurrNet;
using PurrNet.Modules;

public class SceneLoaderRF : NetworkBehaviour
{
    public static SceneLoaderRF Instance { get; private set; }

    private bool isLoading = false;
    private float currentFakeProgress = 0f;

    [Header("Multiplayer Logic")]
    [PurrScene] public string targetSceneName;
    public SessionData sessionData;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider progressBar;

    [Header("Loading logic")]
    [SerializeField] private float fakeLoadSpeed = 1.0f;
    [SerializeField] private float fakeLoadTime = 3.5f;

    [Header("Events")]
    public Action OnLoadStarted;
    public Action<float> OnLoadProgress;
    public Action OnLoadFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        HideUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoadedLocally;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedLocally;
    }

    /// <summary>
    /// Call this from a button or game event. 
    /// It validates the session and initiates the network scene load.
    /// </summary>
    public void TryLoadMultiplayerScene()
    {
        // 1. Only the server should dictate a networked scene change
        if (!isServer)
        {
            Debug.LogWarning("Only the Server/Host can trigger a networked scene change.");
            return;
        }

        if (sessionData == null)
        {
            Debug.LogError("Session Data is NULL");
            return;
        }

        if (sessionData.AllPlayersReady && sessionData.AllPlayersReadyInElevator)
        {

            RpcShowLoadingScreen();

            PurrSceneSettings settings = new()
            {
                isPublic = true,
                mode = LoadSceneMode.Single,
            };

            networkManager.sceneModule.LoadSceneAsync(targetSceneName, settings);
        }
        else
        {
            Debug.Log("Cannot load: Players are not ready.");
        }
    }

    /// <summary>
    /// Tells all clients (including the host) to bring up their UI and start the progress bar.
    /// </summary>
    [ObserversRpc(runLocally: true)]
    private void RpcShowLoadingScreen()
    {
        if (isLoading) return;

        StartCoroutine(LocalVisualLoadingCoroutine());
    }

    /// <summary>
    /// Handles the visual progress bar locally for each player while PurrNet handles the actual loading in the background.
    /// </summary>
    private IEnumerator LocalVisualLoadingCoroutine()
    {
        isLoading = true;
        ShowUI();
        SetProgress(0f);
        currentFakeProgress = 0f;

        OnLoadStarted?.Invoke();

        while (isLoading)
        {
            if (currentFakeProgress < 0.99f)
            {
                float progressStep = Time.deltaTime * fakeLoadSpeed;
                currentFakeProgress += progressStep;
                SetProgress(currentFakeProgress);

                OnLoadProgress?.Invoke(currentFakeProgress);
            }

            yield return null;
        }
    }

    /// <summary>
    /// Triggered automatically on every client when Unity finishes loading the new scene.
    /// </summary>
    private void OnSceneLoadedLocally(Scene scene, LoadSceneMode mode)
    {
        if (isLoading)
        {
            StartCoroutine(FinishLoadingVisuals());
        }
    }

    private IEnumerator FinishLoadingVisuals()
    {
        SetProgress(1f);
        OnLoadProgress?.Invoke(1f);

        yield return new WaitForSeconds(0.5f);

        isLoading = false;
        HideUI();
        OnLoadFinished?.Invoke();
    }

    #region Helpers
    private void ShowUI()
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);
    }

    private void HideUI()
    {
        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    private void SetProgress(float value)
    {
        if (progressBar != null) progressBar.value = Mathf.Clamp01(value);
    }
    #endregion
}
