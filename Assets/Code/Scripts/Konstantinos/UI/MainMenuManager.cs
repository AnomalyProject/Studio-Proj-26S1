using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    [SerializeField] private CollectibleSO tutorialCollectible;

    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button firstSelectedButtonStart;
    [SerializeField] private Button firstSelectedButtonStartTutorial;

    [SerializeField] private GameObject modePanel;
    [SerializeField] private Button firstSelectedButtonMode;

    [SerializeField] private GameObject coopPanel;
    [SerializeField] private Button firstSelectedButtonCoop;

    [SerializeField] private GameObject joinPanel;
    [SerializeField] private Button firstSelectedButtonJoin;

    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;

    [SerializeField] private GameObject PasswordProtectedPanel;
    [SerializeField] private Button firstSelectedButtonPP;

    [SerializeField] private GameObject InputPasswordPanel;
    [SerializeField] private TMP_InputField PasswordInputField;

    [SerializeField] private GameObject notesPanel;

    [Space(10)]
    [Header("Manager Settings")]
    [SerializeField] private bool enableOnStart = true;

    public static MainMenuManager Instance;

    private void Awake()
    {
        Instance = this;

        // Unlock 'Start' tab if you have collectible from tutorial
        UnlockMainGame();
        SetMenuActivity(false);

        //DevConsole.RegisterCommand("skiptutorial", new DevConsole.CommandData("Registers the tutorial as complete.", args =>
        //{
        //    RefrenceManager.CurrentSave.collectiblesGathered.Add(tutorialCollectible.ID);
        //    SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        //    UnlockMainGame();
        //}, "Awards the user with the Tutorial collectible, unlocking the main game."));
    }
    private void UnlockMainGame()
    {
        firstSelectedButtonStart.interactable = true;
        firstSelectedButtonStart.image.color = Color.white;
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
                EventSystem.current.SetSelectedGameObject(firstSelectedButtonStart.IsInteractable() ? firstSelectedButtonStart.gameObject : firstSelectedButtonStartTutorial.gameObject);
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
                ResetPanels();
                startPanel.SetActive(true);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonStart.gameObject);
                break;
            case 1:
                ResetPanels();
                modePanel.SetActive(true);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonMode.gameObject);
                break;
            case 2:
                ResetPanels();
                coopPanel.SetActive(true);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonCoop.gameObject);
                break;
            case 3:
                ResetPanels();
                joinPanel.SetActive(true);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonJoin.gameObject);
                break;
            case 4:
                ResetPanels();
                PasswordProtectedPanel.SetActive(true);

                EventSystem.current.SetSelectedGameObject(firstSelectedButtonPP.gameObject);
                break;
            case 5:
                ResetPanels();
                InputPasswordPanel.SetActive(true);

                EventSystem.current.SetSelectedGameObject(PasswordInputField.gameObject);
                break;
            case 6:
                ResetPanels();
                notesPanel.SetActive(true);
                break;
        }
    }

    private void ResetPanels()
    {
        startPanel.SetActive(false);
        modePanel.SetActive(false);
        coopPanel.SetActive(false);
        joinPanel.SetActive(false);
        PasswordProtectedPanel.SetActive(false);
        InputPasswordPanel.SetActive(false);
        notesPanel.SetActive(false);
    }
    public void StartGame()
    {
        StartCoroutine(StartGameWithDelay());
    }
    private IEnumerator StartGameWithDelay()
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
    public void StartTutorial()
    {
        StartCoroutine(StartTutorialWithDelay());
    }
    private IEnumerator StartTutorialWithDelay()
    {
        SetMenuActivity(false);
        SettingsManager.Instance?.SwitchCanvasMode(RenderMode.ScreenSpaceOverlay); // makes Settings screen space
        mainCameraAnim?.SetTrigger("Play");
        yield return new WaitForSeconds(mainCameraAnimDuration / 1.5f);
        if (ElevatorDoorAnim != null) ElevatorDoorAnim.enabled = true;
        yield return new WaitForSeconds(mainCameraAnimDuration / 2.5f);
        if (BlackFadeManager.Instance != null) BlackFadeManager.Instance?.FadeIn();
        SessionModeManager.Instance.StartSoloInScene("Tutorial");
    }

    public void HostCoOp()
    {
        StartCoroutine(HostCoOpWithDelay());
    }
    private IEnumerator HostCoOpWithDelay()
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

    private IEnumerator JoinCoOpWithDelay(ulong lobbyId)
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

    public void ShowMessage(string message)
    {
        if (messagePanel == null || messageText == null) return;

        messagePanel.SetActive(true);
        messageText.text = message;
    }
}