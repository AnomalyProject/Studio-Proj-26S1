using PurrNet;
using PurrNet.Modules; 
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerSceneLoader : NetworkBehaviour
{
    [PurrScene] public string sceneName;


    [ContextMenu("Change Scene")]
    private void ChangeScene()
    {
        PurrSceneSettings settings = new()
        {
            isPublic = true,
            mode = LoadSceneMode.Single,
        };

        networkManager.sceneModule.LoadSceneAsync(sceneName);
    }
}
