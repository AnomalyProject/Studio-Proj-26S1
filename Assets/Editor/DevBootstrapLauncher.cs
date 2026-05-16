using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

public static class DevBootstrapLauncher
{
    private const string BootstrapScenePath = "Assets/Scenes/Christina/Bootstrap.unity";

    /// <summary>
    /// Saves open scenes, writes the caller's handoff data, points
    /// playModeStartScene at Bootstrap, and enters Play Mode.
    /// writeHandoff runs only AFTER a successful save, so a cancelled save
    /// never leaves a stale handoff behind.
    /// </summary>
    public static void EnterPlayModeThroughBootstrap(Action writeHandoff)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        
        Scene current = EditorSceneManager.GetActiveScene();
        if (current.isDirty && !EditorSceneManager.SaveOpenScenes())
        {
            Debug.LogWarning("[DevBootstrap] Aborted. User cancelled save.");
            return;
        }
        
        SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        if (bootstrap == null)
        {
            Debug.LogError($"[DevBootstrap] Bootstrap scene not found at '{BootstrapScenePath}'.");
            return;
        }
        
        writeHandoff?.Invoke();

        EditorSceneManager.playModeStartScene = bootstrap;
        EditorApplication.isPlaying = true;
    }
}
