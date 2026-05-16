using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DevMultiplayerMenu
{
    [MenuItem("Dev/Multiplayer/Host Current Scene")]
    public static void HostCurrentScene()
    {
        var request = new DevBootstrapRequest
        {
            mode = DevLaunchMode.DevHost,
            scenePath = EditorSceneManager.GetActiveScene().path,
            address = "127.0.0.1",
            port = 5000,
            maxPlayers = 4,
            playerIndex = 0,
        };
        Launch(request);
    }
    
    [MenuItem("Dev/Multiplayer/Join host")]
    public static void JoinLocalhost()
    {
        var request = new DevBootstrapRequest
        {
            mode = DevLaunchMode.DevClient,
            address = "127.0.0.1",
            port = 5000,
            playerIndex = 1,
        };
        Launch(request);
    }
    
    private static void Launch(DevBootstrapRequest request)
    {
        DevBootstrapLauncher.EnterPlayModeThroughBootstrap(() =>
        {
            string json = JsonUtility.ToJson(request);
            EditorPrefs.SetString(DevBootstrapRequest.LaunchRequestPrefKey, json);
            EditorPrefs.DeleteKey(DevBootstrapRequest.LegacyDevScenePrefKey);
            Debug.Log($"[DevMultiplayer] Launch request written: {json}");
        });
    }
}
