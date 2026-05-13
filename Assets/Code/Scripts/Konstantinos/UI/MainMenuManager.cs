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
    [SerializeField] private GameObject firstSelectedButtonStart;

    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject firstSelectedButtonMode;

    [SerializeField] private GameObject coopPanel;
    [SerializeField] private GameObject firstSelectedButtonCoop;

    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject firstSelectedButtonJoin;

    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;

    [Space(10)]
    [Header("Manager Settings")]
    [SerializeField] private bool enableOnStart = true;

    public static MainMenuManager instance;

    private void Awake()
    {
        instance = this;
        SetMenuActivity(false);
    }
    private void OnDestroy() => instance = null;

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

    private IEnumerator Start()
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
        SessionModeManager.Instance.StartSolo();
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