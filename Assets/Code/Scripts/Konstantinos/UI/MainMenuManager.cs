using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
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
    [SerializeField] CinemachineCamera mainCamera;
    [SerializeField] Animator mainCameraAnim;
    [SerializeField] Animator ElevatorDoorAnim;
    [SerializeField] float mainCameraAnimDuration = 8.0f;
    [SerializeField] AudioClip mainMenuMusic;

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

    public static MainMenuManager Instance;

    private void Awake()
    {
        Instance = this;
        SetMenuActivity(false);
    }
    private void OnDestroy() => Instance = null;

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
        BlackFadeManager.Instance?.FadeOut();
        yield return null;
        if (!enableOnStart) yield break;

        SetMenuActivity(true);
        SetPanel(0);
        SettingsManager.Instance?.CaptureWorldTransform();
        SettingsManager.Instance?.SwitchCanvasMode(RenderMode.WorldSpace); // makes Settings world space
        if (mainMenuMusic != null) AudioManager.Instance?.PlayMusic(mainMenuMusic);
    }

    public void SetMenuActivity(bool active)
    {
        switch (active)
        {
            case true:
                // activate canvas and select first button
                mainMenuCanvas?.SetActive(true);
                mainCamera?.Prioritize();
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
        StartCoroutine(StartGameWithDelay());
    }
    IEnumerator StartGameWithDelay()
    {
        SetMenuActivity(false);
        SettingsManager.Instance?.SwitchCanvasMode(RenderMode.ScreenSpaceOverlay); // makes Settings screen space
        mainCameraAnim?.SetTrigger("Play");
        yield return new WaitForSeconds(mainCameraAnimDuration/ 1.5f);
        if (ElevatorDoorAnim != null) ElevatorDoorAnim.enabled = true;
        yield return new WaitForSeconds(mainCameraAnimDuration / 2.5f);
        if (BlackFadeManager.Instance != null) BlackFadeManager.Instance?.FadeIn();
        SessionModeManager.Instance.StartSolo();
    }

    public void HostCoOp()
    {
        StartCoroutine(HostCoOpWithDelay());
    }
    IEnumerator HostCoOpWithDelay()
    {
        SetMenuActivity(false);
        SettingsManager.Instance?.SwitchCanvasMode(RenderMode.ScreenSpaceOverlay); // makes Settings screen space
        mainCameraAnim.SetTrigger("Play");
        yield return new WaitForSeconds(mainCameraAnimDuration / 1.5f);
        if (ElevatorDoorAnim != null) ElevatorDoorAnim.enabled = true;
        yield return new WaitForSeconds(mainCameraAnimDuration / 2.5f);
        if (BlackFadeManager.Instance != null) BlackFadeManager.Instance?.FadeIn();
        SessionModeManager.Instance.StartHosting();
    }

    public void JoinCoOp(ulong lobbyId)
    {
       StartCoroutine(JoinCoOpWithDelay(lobbyId));
    }

    IEnumerator JoinCoOpWithDelay(ulong lobbyId)
    {
        SetMenuActivity(false);
        SettingsManager.Instance?.SwitchCanvasMode(RenderMode.ScreenSpaceOverlay); // makes Settings screen space
        mainCameraAnim.SetTrigger("Play");
        yield return new WaitForSeconds(mainCameraAnimDuration / 1.5f);
        if (ElevatorDoorAnim != null) ElevatorDoorAnim.enabled = true;
        yield return new WaitForSeconds(mainCameraAnimDuration / 2.5f);
        if (BlackFadeManager.Instance != null) BlackFadeManager.Instance?.FadeIn();
        SteamSessionBridge.Instance.RequestJoinLobbyById(lobbyId);
    }

    public void OpenSettings()
    {
        SetMenuActivity(false);
        SettingsManager.Open(); 
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#endif
    }
    #endregion

    private void ShowMessage(string message)
    {
        if (messagePanel == null || messageText == null) return;

        messagePanel.SetActive(true);
        messageText.text = message;
    }
}