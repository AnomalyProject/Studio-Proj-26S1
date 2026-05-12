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

    public void TryLoadMultiplayerScene()
    {
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
            StartCoroutine(ServerLoadSequence());
        }
        else
        {
            Debug.Log("Cannot load: Players are not ready.");
        }
    }

    /// <summary>
    /// Handles the timing so the RPC reaches clients before the scene change destroys/pauses everything.
    /// </summary>
    private IEnumerator ServerLoadSequence()
    {
        RpcShowLoadingScreen();

        yield return new WaitForSeconds(0.3f);

        PurrSceneSettings settings = new()
        {
            isPublic = true,
            mode = LoadSceneMode.Single,
        };

        networkManager.sceneModule.LoadSceneAsync(targetSceneName, settings);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcShowLoadingScreen()
    {
        if (isLoading) return;
        StartCoroutine(LocalVisualLoadingCoroutine());
    }

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