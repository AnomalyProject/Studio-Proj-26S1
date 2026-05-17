using System.Collections;
using System.IO;
using UnityEngine;

public class BootstrapController : MonoBehaviour
{
    [SerializeField] private string defaultFirstScene = "MainMenuChristina";
    [SerializeField] private BootstrapSceneRegistry sceneRegistry;

    void Start()
    {
#if UNITY_EDITOR
        // using the JSON launch request
        string requestJson = UnityEditor.EditorPrefs.GetString(DevBootstrapRequest.LaunchRequestPrefKey, string.Empty);
        UnityEditor.EditorPrefs.DeleteKey(DevBootstrapRequest.LaunchRequestPrefKey);

        if (!string.IsNullOrEmpty(requestJson))
        {
            StartCoroutine(BootFromLaunchRequest(requestJson));
            return;
        }
        
        string devScenePath  = UnityEditor.EditorPrefs.GetString(DevBootstrapRequest.LegacyDevScenePrefKey, string.Empty);
        UnityEditor.EditorPrefs.DeleteKey(DevBootstrapRequest.LegacyDevScenePrefKey);
        
        if (!string.IsNullOrEmpty(devScenePath))
        {
            StartCoroutine(BootIntoDevScene(devScenePath));
            return;
        }
#endif
        SceneLoader.Instance.LoadScene(defaultFirstScene);
    }
    
    private IEnumerator BootIntoDevScene(string scenePath)
    {
        // yield one frame so Unity's scene activation fully settles before we trigger another load
        yield return null;
        
        if (sceneRegistry == null)
        {
            Debug.LogError("[BootstrapController] Scene registry is missing. Loading main menu instead...");
            SceneLoader.Instance.LoadScene(defaultFirstScene);
            yield break;
        }
        
        if (!sceneRegistry.TryGetScene(scenePath, out BootstrapSceneEntry entry))
        {
            Debug.LogWarning($"[BootstrapController] Scene '{scenePath}' is not registered for bootstrap. Loading main menu instead.");
            SceneLoader.Instance.LoadScene(defaultFirstScene);
            yield break;
        }
        
        // important for the team to know just in case
        if (SessionModeManager.Instance == null)
        {
            Debug.LogError("[BootstrapController] SessionModeManager is missing. Cannot boot into dev scene.");
            yield break;
        }

        SessionModeManager.Instance.StartSoloInScene(entry.runtimeSceneName);
    }
    
    private IEnumerator BootFromLaunchRequest(string requestJson)
    {
        // yield one frame so Unity's scene activation fully settles before we trigger another load
        yield return null;

        DevBootstrapRequest request = null;
        try
        {
            request = JsonUtility.FromJson<DevBootstrapRequest>(requestJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BootstrapController] Could not parse launch request: {e.Message}. Loading main menu.");
        }

        if (request == null)
        {
            SceneLoader.Instance.LoadScene(defaultFirstScene);
            yield break;
        }

        if (SessionModeManager.Instance == null)
        {
            Debug.LogError("[BootstrapController] SessionModeManager is missing. Cannot boot dev session.");
            yield break;
        }

        switch (request.mode)
        {
            case DevLaunchMode.Solo:      BootSolo(request);      break;
            case DevLaunchMode.DevHost:   BootDevHost(request);   break;
            case DevLaunchMode.DevClient: BootDevClient(request); break;
            default:
                Debug.LogError($"[BootstrapController] Unknown launch mode '{request.mode}'. Loading main menu.");
                SceneLoader.Instance.LoadScene(defaultFirstScene);
                break;
        }
    }
    
    private bool TryResolveScene(string scenePath, out BootstrapSceneEntry entry)
    {
        entry = null;

        if (sceneRegistry == null)
        {
            Debug.LogError("[BootstrapController] Scene registry is missing. Loading main menu instead...");
            SceneLoader.Instance.LoadScene(defaultFirstScene);
            return false;
        }

        if (!sceneRegistry.TryGetScene(scenePath, out entry))
        {
            Debug.LogWarning($"[BootstrapController] Scene '{scenePath}' is not registered for bootstrap. Loading main menu instead.");
            SceneLoader.Instance.LoadScene(defaultFirstScene);
            return false;
        }

        return true;
    }
    
    private void BootSolo(DevBootstrapRequest request)
    {
        if (!TryResolveScene(request.scenePath, out BootstrapSceneEntry entry)) return;
        SessionModeManager.Instance.StartSoloInScene(entry.runtimeSceneName);
    }
    
    private void BootDevHost(DevBootstrapRequest request)
    {
        if (!TryResolveScene(request.scenePath, out BootstrapSceneEntry entry)) return;
        SessionModeManager.Instance.StartDevHost(entry.runtimeSceneName, request);
    }

    private void BootDevClient(DevBootstrapRequest request)
    {
        SessionModeManager.Instance.StartDevClient(request);
    }
}
