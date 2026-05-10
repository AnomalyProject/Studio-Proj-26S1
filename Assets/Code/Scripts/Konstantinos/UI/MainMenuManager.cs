using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    enum SceneLoadingMethod
    {
        WithString,
        WithIndex
    }

    [Header("References")]
    [SerializeField] GameObject mainMenuCanvas;
    
    [SerializeField] private GameObject startPanel;
    [SerializeField] GameObject firstSelectedButtonStart;

    [SerializeField] private GameObject modePanel;
    [SerializeField] GameObject firstSelectedButtonMode;

    [SerializeField] private GameObject coopPanel;
    [SerializeField] GameObject firstSelectedButtonCoop;

    [SerializeField] private GameObject joinPanel;
    [SerializeField] GameObject firstSelectedButtonJoin;

    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;

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

    private void OnEnable()
    {
        SettingsManager.OnSettingsClosed += HandleSettingsClosed;

        string message = SessionModeManager.Instance.LastJoinFailureMessage;

        if (!String.IsNullOrWhiteSpace(message))
        {
            ShowMessage(message);
            SessionModeManager.Instance.ClearLastJoinFailureMessage();
        }
    }

    private void OnDisable()
    {
        SettingsManager.OnSettingsClosed -= HandleSettingsClosed;
    }

    private void HandleSettingsClosed()
    {
        SetMenuActivity(true);
    }

    IEnumerator Start()
    {
        if (enableOnStart)
        {
            SetMenuActivity(true);
        }

        yield return null;
        SetPanel(0);
    }

    public void SetMenuActivity(bool active)
    {
        switch (active)
        {
            case true:
                // activate canvas and select first button
                mainMenuCanvas?.SetActive(true);
                EventSystem.current.SetSelectedGameObject(firstSelectedButtonStart);
                break;
            case false:
                // deactivate canvas and clear selected button
                mainMenuCanvas?.SetActive(false);
                EventSystem.current.SetSelectedGameObject(null);
                break;
        }
    }

    #region Canvas Button Methods

    public void SetPanel(int index)
    {
        switch (index)
        {
            case 0:
                startPanel.SetActive(true);
                modePanel.SetActive(false);
                coopPanel.SetActive(false);
                joinPanel.SetActive(false);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonStart);
                break;
            case 1:
                startPanel.SetActive(false);
                modePanel.SetActive(true);
                coopPanel.SetActive(false);
                joinPanel.SetActive(false);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonMode);
                break;
            case 2:
                startPanel.SetActive(false);
                modePanel.SetActive(false);
                coopPanel.SetActive(true);
                joinPanel.SetActive(false);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonCoop);
                break;
            case 3:
                startPanel.SetActive(false);
                modePanel.SetActive(false);
                coopPanel.SetActive(false);
                joinPanel.SetActive(true);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonJoin);
                break;
        }
    }

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

    public void HostCoOp()
    {
        SessionModeManager.Instance.StartHosting();
    }

    public void OpenSettings()
    {
        SetMenuActivity(false);
        SettingsManager.Open(); 
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Player has quit the game!");
    }
    #endregion

    private void ShowMessage(string message)
    {
        if (messagePanel == null || messageText == null) return;

        messagePanel.SetActive(true);
        messageText.text = message;
    }
}