using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the user interface and workflow for hosting and joining password-protected multiplayer lobbies.
/// </summary>
/// <remarks>This component coordinates the display and validation of password fields when creating or joining a
/// private lobby. It integrates with Steam lobby metadata to indicate password protection and handles user feedback for
/// incorrect password attempts. Use this class to provide a secure lobby experience where access is restricted by a
/// user-defined password. Only one instance of this class should exist at a time; access it via the static Instance
/// property.
/// Uses external scripts like MainMenuManager for scene flow and SteamSessionBridge for lobby management,
/// but keeps password logic self-contained here for clarity and modularity.
/// </remarks>
public class PasswordProtectedLobby : MonoBehaviour
{
    public static PasswordProtectedLobby Instance { get; private set; }

    [Header("Host")]
    [SerializeField] private GameObject hostSetupPanel;
    [SerializeField] private GameObject firstSelectedHostSetup;
    [SerializeField] private TMP_InputField hostPasswordInput;

    [Header("Join")]
    [SerializeField] private GameObject joinPasswordPanel;
    [SerializeField] private GameObject firstSelectedJoinPassword;
    [SerializeField] private TMP_InputField joinPasswordInput;
    [SerializeField] private TMP_Text joinErrorText;

    private ulong pendingJoinLobbyId;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        hostSetupPanel.SetActive(false);
        joinPasswordPanel.SetActive(false);
    }

    public void HostPublic()
    {
        SessionModeManager.Instance.PendingLobbyPassword = "";
        MainMenuManager.Instance.HostCoOp();
    }
    public void OpenHostPasswordPanel()
    {
        hostSetupPanel.SetActive(true);
        hostPasswordInput.text = "";
        EventSystem.current.SetSelectedGameObject(firstSelectedHostSetup);
    }

    public void CloseHostSetupPanel()
    {
        hostSetupPanel.SetActive(false);
    }
    public void ConfirmHostSetup()
    {
        string password = hostPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(password))
        {
            ShowHostError("Please enter a password for your private lobby.");
            return;
        }

        SessionModeManager.Instance.PendingLobbyPassword = password;

        hostSetupPanel.SetActive(false);

        MainMenuManager.Instance.HostCoOp();
    }

    public void OnSteamLobbyCreated(CSteamID lobbyId)
    {
        bool hasPassword = !string.IsNullOrEmpty(SessionModeManager.Instance.PendingLobbyPassword);
        SteamMatchmaking.SetLobbyData(lobbyId, "has_password", hasPassword ? "true" : "false");
        Debug.Log($"[PasswordCoordinator] has_password metadata set to {hasPassword} for lobby {lobbyId}");
    }

    private void ShowHostError(string message)
    {
        MainMenuManager.Instance.ShowMessage(message);
    }

    public void TryJoinLobby(ulong lobbyId)
    {
        string hasPassword = SteamMatchmaking.GetLobbyData(new CSteamID(lobbyId), "has_password");

        if (hasPassword == "true")
        {
            pendingJoinLobbyId = lobbyId;
            OpenJoinPasswordPanel();
            return;
        }

        MainMenuManager.Instance.JoinCoOp(lobbyId);
    }

    private void OpenJoinPasswordPanel()
    {
        joinPasswordPanel.SetActive(true);
        joinPasswordInput.text = "";
        if (joinErrorText != null) joinErrorText.text = "";
        EventSystem.current.SetSelectedGameObject(firstSelectedJoinPassword);
    }

    public void CloseJoinPasswordPanel()
    {
        joinPasswordPanel.SetActive(false);
        pendingJoinLobbyId = 0;
    }
    public void SubmitJoinPassword()
    {
        string attempt = joinPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(attempt))
        {
            ShowJoinError("Please enter the lobby password.");
            return;
        }

        SteamSessionBridge.Instance.SetPendingJoinPassword(attempt);

        joinPasswordPanel.SetActive(false);

        MainMenuManager.Instance.JoinCoOp(pendingJoinLobbyId);
    }

    private void ShowJoinError(string message)
    {
        if (joinErrorText != null)
            joinErrorText.text = message;
    }

    //Error feedback
    private void OnEnable()
    {
        SessionEvents.OnSessionError += OnSessionError;

        if (SteamSessionBridge.Instance != null)
            SteamSessionBridge.Instance.OnHostStartupStatusChanged += OnHostStartupStatusChanged;
    }

    private void OnDisable()
    {
        SessionEvents.OnSessionError -= OnSessionError;

        if (SteamSessionBridge.Instance != null)
            SteamSessionBridge.Instance.OnHostStartupStatusChanged -= OnHostStartupStatusChanged;
    }

    private void OnSessionError(SessionErrorResponse error)
    {
        if (error.Code == SessionErrorCode.InvalidState &&
            error.Message.Contains("password"))
        {
            ShowJoinError("Incorrect password. Try again.");

            OpenJoinPasswordPanel();
        }
    }

    private void OnHostStartupStatusChanged(HostStartupStatus status)
    {
        if (status.Stage == HostStartupStage.LobbyCreated)
        {
            OnSteamLobbyCreated(GetCurrentHostLobbyId());
        }

        if (status.Stage == HostStartupStage.Failed)
        {
            if (SessionModeManager.Instance != null)
                SessionModeManager.Instance.PendingLobbyPassword = "";
        }
    }

    private CSteamID GetCurrentHostLobbyId()
    {
        return SteamSessionBridge.Instance.CurrentLobbyID;
    }
}