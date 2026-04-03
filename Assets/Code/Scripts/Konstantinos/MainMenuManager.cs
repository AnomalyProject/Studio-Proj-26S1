using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject mainMenuCanvas;
    [SerializeField] GameObject firstSelectedButton;

    [Space(10)]
    [Header("Manager Settings")]
    [SerializeField] bool enableOnStart = true;

    [Space(10)]
    [Header("Start Settings")]
    [SerializeField] SceneLoadingMethod currentSceneLoading;
    [SerializeField] string startSceneString = "MainGameplayScene";
    [SerializeField] int startSceneIndex = 1;


    private void Awake()
    {
        SetMenuActivity(false);
    }

    void Start()
    {
        if (enableOnStart)
        {
            SetMenuActivity(true);
        }
    }

    public void SetMenuActivity(bool active)
    {
        switch (active)
        {
            case true:
                // activate canvas and select first button
                mainMenuCanvas?.SetActive(true);
                EventSystem.current.SetSelectedGameObject(firstSelectedButton);
                break;
            case false:
                // deactivate canvas and clear selected button
                mainMenuCanvas?.SetActive(false);
                EventSystem.current.SetSelectedGameObject(null);
                break;
        }
    }

    #region Canvas Button Methods
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
    #endregion
}


enum SceneLoadingMethod
{
    WithString,
    WithIndex
}
