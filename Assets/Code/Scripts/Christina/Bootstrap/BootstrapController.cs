using System.Collections;
using System.IO;
using UnityEngine;

public class BootstrapController : MonoBehaviour
{
    [SerializeField] private string defaultFirstScene = "MainMenuChristina";
    [SerializeField] private BootstrapSceneRegistry sceneRegistry;
    
    // a key used to store the selected devs cene in the Unity EditorPrefs
    private const string DevScenePrefKey = "Christina.DevScenePath";
    
    void Start()
    {
#if UNITY_EDITOR
        string devScenePath  = UnityEditor.EditorPrefs.GetString(DevScenePrefKey, string.Empty);
        UnityEditor.EditorPrefs.DeleteKey(DevScenePrefKey);
        
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
}
