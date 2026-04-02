using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject mainMenuCanvas;


    [Space(10)]
    [Header("Start Settings")]
    [SerializeField] SceneLoadingMethod currentSceneLoading;
    [SerializeField] string startSceneString = "MainGameplayScene";
    [SerializeField] int startSceneIndex = 1;


    // Canvas Button Methods
    public void StartGame()
    {
        switch (currentSceneLoading)
        {
            case SceneLoadingMethod.WithIndex:
                SceneLoader.Instance.LoadSceneWithAsync(startSceneIndex); 
                break;
            case SceneLoadingMethod.WithString:
                SceneLoader.Instance.LoadSceneWithAsync(startSceneString);
                break;

        }
    }

    public void OpenSettings()
    {
        // SettingsManager.Open() 
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}


enum SceneLoadingMethod
{
    WithString,
    WithIndex
}
