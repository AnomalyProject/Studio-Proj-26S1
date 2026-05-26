using PurrNet;
using PurrNet.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderRF : SceneLoader
{

    [Header("Multiplayer Logic")]
    [PurrScene] private string targetSceneName;
    public SessionData sessionData;

    protected override void Awake()
    {
        base.Awake();
        targetSceneName = "MainGameplayScene";

        DevConsole.CommandData data = new("loads a scene", LoadSceneCheat);
        DevConsole.RegisterCommand("load", data);
        Debug.Log("Registered command");
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        networkManager.sceneModule.onPostSceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        networkManager.sceneModule.onPostSceneLoaded -= OnSceneLoaded;
    }

    private void LoadSceneCheat(string[] sceneArg)
    {
        LoadSceneServer(sceneArg[0]);
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
            LoadSceneServer(targetSceneName);
        }
        else
        {
            Debug.Log("Cannot load: Players are not ready.");
        }
    }

    /// <summary>
    /// Handles the timing so the RPC reaches clients before the scene change destroys/pauses everything.
    /// </summary>
    public async void LoadSceneServer(string name)
    {
        if (!isServer) return;
        SceneLoadingRPC(name);

        PurrSceneSettings settings = new()
        {
            isPublic = true,
            mode = LoadSceneMode.Single,
        };
        PerformAsyncOperation(networkManager.sceneModule.LoadSceneAsync(name, settings));
    }
    private void OnSceneLoaded(SceneID scene, bool asServer) => HideUI();
    [ObserversRpc] private void SceneLoadingRPC(string sceneName) => ShowUI();
}