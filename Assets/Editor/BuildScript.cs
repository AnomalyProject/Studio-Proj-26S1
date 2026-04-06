using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript 
{
    [MenuItem("Build/Perform Build")]
    public static void PerformBuild() 
    {
        string[] activeScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        if (activeScenes.Length == 0) {
            Debug.LogError("Alfred: I cannot find any enabled scenes in Build Settings, Sir!");
            Debug.LogError("Please ensure your scenes are added and CHECKED in File > Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        string buildPath = "Builds/Studio-Proj-26S1.exe";

        Debug.Log($"Buttler Jeeves: Starting the build process for {activeScenes.Length} scenes, Sir...");
        foreach (var scene in activeScenes) {
            Debug.Log($"Alfred: Packing scene: {scene}");
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = activeScenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded) {
            Debug.Log($"Alfred: Build Finished, Result: Success. Size: {summary.totalSize / 1024 / 1024} MB");
        }

        if (summary.result == BuildResult.Failed) {
            Debug.LogError("Alfred: Build Finished, Result: Failure");
            EditorApplication.Exit(1);
        }
    }
}