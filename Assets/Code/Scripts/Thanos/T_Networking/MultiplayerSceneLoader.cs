using PurrNet;
using PurrNet.Modules; 
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerSceneLoader : NetworkBehaviour
{
    [PurrScene] public string sceneName;

    SessionData sessionData;


    [ObserversRpc(excludeSender: true, runLocally: true)]
    private void ChangeScene()
    {
        if (sessionData == null)
        {
            Debug.LogError("Session Data is NULL");
            return;
        }

        if(sessionData.AllPlayersReady && sessionData.AllPlayersReadyInElevator)
        {

            PurrSceneSettings settings = new()
            {
                isPublic = true,
                mode = LoadSceneMode.Single,
            };

            networkManager.sceneModule.LoadSceneAsync(sceneName);
        }
        else
        {
            Debug.Log("Players were not ready");
        }
    }
}
