using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript {
    [MenuItem("Build/Perform Build")]
    public static void PerformBuild() {
        /* string[] activeScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
         string buildPath = "Builds/studio-proj-26S1.exe";

         Debug.Log("Buttler Jeeves: Starting the build process, Sir...");

         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
         buildPlayerOptions.scenes =activeScenes;
         buildPlayerOptions.locationPathName = buildPath;
         buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
         buildPlayerOptions.options = BuildOptions.None;

         BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
         BuildSummary summary = report.summary;

         if (summary.result == BuildResult.Succeeded) {
             Debug.Log("Buttler Jeeves: Build Finished, Result: Success");
         }

         if (summary.result == BuildResult.Failed) {
             Debug.Log("Buttler Jeeves: Build Finished, Result: Failure");
             System.Console.Error.WriteLine("Unity Build Failed!");
             //Make buttler exit with an error code to indicate failure  
             UnityEditor.EditorApplication.Exit(1);*/
        string[] activeScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        BuildPipeline.BuildPlayer(activeScenes, "Builds/studio-proj-26S1.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
    }
    }