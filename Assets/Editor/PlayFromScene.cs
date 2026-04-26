using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor helper for testing the game from any scene while stills tarting through Bootstrap.
/// If the curent scene is not Bootstrap, this script stores the current scene in EditorPrefs,
/// tells Unity to enter Play Mode from the Bootstrap scene and then starts Play Mode.
/// It also includes menu options to clear or inspect the saved dev scene override.
/// </summary>
[InitializeOnLoad]
public static class PlayFromScene
{
    // a key used to store the selected devs cene in the Unity EditorPrefs
    private const string DevScenePrefKey = "Christina.DevScenePath";
    
    // Bootstrap scene path that should always be used when enterint play mode. 
    private const string BootstrapScenePath = "Assets/Scenes/Christina/Bootstrap.unity";
   // private const string RegistryAssetPath = "Assets/Code/Scripts/Christina/Bootstrap/BootstrapSceneRegistry.asset";

    /// <summary>
    /// Starts PlayMode through Bootstrap scene. If already playing it stops Play Mode.
    /// If the current scene is Bootstrap, it just plays normally. Otherwise it saves the current
    /// scene path, sets Bootstrap as the startup scene and enters Play Mode form there. 
    /// </summary>
    [MenuItem("Dev/Play From Current Scene %#m")] // %#p is Ctrl + Shift + M
    public static void Play()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        Scene currentScene = EditorSceneManager.GetActiveScene();

        if (currentScene.name == "Bootstrap")
        {
            EditorApplication.isPlaying = true;
            return;
        }

        // in case there are unsaved changes in the scene, save them. 
        if (currentScene.isDirty && !EditorSceneManager.SaveOpenScenes())
        {
            Debug.LogWarning("[PlayFromScene] Aborted. User cancelled save.");
            return;
        }
        
        EditorPrefs.SetString(DevScenePrefKey, currentScene.path);
        
        SceneAsset bootstrapAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        if (bootstrapAsset == null)
        {
            Debug.LogError($"[PlayFromScene] Bootstrap scene not found at '{BootstrapScenePath}'.");
            return;
        }
        
        EditorSceneManager.playModeStartScene = bootstrapAsset;
        EditorApplication.isPlaying = true;
    }

    /// <summary>
    /// Clears the saved dev scene from the EditorPlefs. After this,
    /// bootstrap will use its normal default scene flow
    /// </summary>
    [MenuItem("Dev/Play Clear Dev Scene Override")]
    public static void Clear()
    {
        EditorPrefs.DeleteKey(DevScenePrefKey);
        Debug.Log("[PlayFromScene] Dev scene override cleared. Bootstrap will load MainMenu.");
    }
    
    /// <summary>
    /// Print the currently saved dev scene to the console. Useful for checking whether a scene is currently stored.
    /// </summary>
    [MenuItem("Dev/Play Show Current Dev Scene")]
    public static void Show()
    {
        string scene = EditorPrefs.GetString(DevScenePrefKey, "");
        Debug.Log(string.IsNullOrEmpty(scene) ? "[PlayFromScene] No dev scene set so Bootstrap will load MainMenu." : $"[PlayFromScene] Dev scene: {scene}");
    }

    /// <summary>
    /// Registers Play Mode state callback when the editor loads this class.
    /// </summary>
    static PlayFromScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>
    /// Resets the temporary Play Mode start scene after returning to Edit Mode. This prevents bootstrao from
    /// staying forced as the startup scene forever.
    /// </summary>
    /// <param name="state"></param>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
