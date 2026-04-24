using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Networking/Bootstrap Scene Registry", fileName = "BootstrapSceneRegistry")]
public class BootstrapSceneRegistry : ScriptableObject
{
    [SerializeField] private List<BootstrapSceneEntry> scenes = new();
    
    public bool TryGetScene(string scenePath, out BootstrapSceneEntry entry)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            BootstrapSceneEntry candidate = scenes[i];

            if (candidate.supportsBootstrap && candidate.scenePath == scenePath)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }
    
}

[Serializable]
public class BootstrapSceneEntry
{
    public string displayName;
    public string scenePath;
    public string runtimeSceneName;
    public bool supportsBootstrap = true;
    public bool requiresSpawnPoint = true;
}
