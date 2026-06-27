using System;
using System.Collections;
using PurrNet;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Single source of truth for the anomaly game flow.
/// Server authority over game state, listens to player decisions and room state changes to react accordingly.
/// Listens to <see cref="MapOrientor.OnElevatorInteracted"/> for player decisions,
/// and to <see cref="AnomalyManager.OnStateChanged"/> to react to room transitions.
///
/// Correct decisions:
///   - Anomaly present  → player takes the ENTRY elevator (goes back)
///   - No anomaly       → player takes the EXIT elevator (continues forward)
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    #region Inspector
    [Header("Dependencies")]
    [SerializeField] private AnomalyManager anomalyManager;
    [SerializeField] private MapOrientor mapOrientor;

    [Header("Win Condition")]
    [Tooltip("How many correct decisions in a row are required to win.")]
    [SerializeField] private int requiredCorrectDecisions = 5;

    [Header("Decision Cooldown")]
    [Tooltip("Seconds after a decision is made before another can be registered. Prevents spam.")]
    [SerializeField, Min(1)] private float decisionCooldown = 2f;

    [Tooltip("Seconds between elevator interaction and map change. Allows time for animations, feedback, etc.")]
    [SerializeField, Min(.5f)] private float mapChangeDelay = 2;

    [Tooltip("Seconds the player has to exit the punishment room before progress resets.")]
    [SerializeField, Min(1f)] private float punishmentTimeLimit = 10;
    #endregion

    #region State
    public int CurrentProgress { get; private set; }
    private bool ElevatorCoolDown;
    private bool isFirstRoom => CurrentProgress == 0;

    private Coroutine punishmentTimerCoroutine;
    private Coroutine mapChangeCoroutine;

    // Pending elevator interaction state, applied once the doors finish animating.
    // Set on the server via QueueElevatorInteraction; consumed on the server via
    // ApplyPendingElevatorInteraction, which is wired in the Inspector to each
    // ElevatorExit's OnFullyOpened UnityEvent.
    private bool pendingEntryEnabled;
    private bool pendingExitEnabled;
    private bool hasPendingInteraction;

    public AnomalyManager AnomalyManager => anomalyManager;
    #endregion

    #region Events
    public static event Action<GameManager> OnInitialized, OnDestroyed;

    public UnityEvent<int> OnProgressChanged;
    public UnityEvent<float> OnPunishmentTimerTick;
    public UnityEvent OnWrongDecision, OnPunishmentTimerExpired;
    public UnityEvent OnGameWon;
    public UnityEvent OnGameReset;

    [ObserversRpc] void InvokeOnWrongDecision() => OnWrongDecision?.Invoke();
    [ObserversRpc] void InvokeOnGameWon() => OnGameWon?.Invoke();
    [ObserversRpc] void InvokeOnGameReset() => OnGameReset?.Invoke();
    [ObserversRpc] void InvokeOnProgressChanged(int prog) => OnProgressChanged?.Invoke(prog);
    #endregion

    #region Unity Lifecycle
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        ValidateDependencies();
        Instance = this;
        OnInitialized?.Invoke(this);

        if (!asServer) return;
        NewGame();
    }

    private void OnEnable()
    {
        MapOrientor.OnElevatorInteracted += HandleElevatorInteracted;
        anomalyManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        MapOrientor.OnElevatorInteracted -= HandleElevatorInteracted;
        anomalyManager.OnStateChanged -= HandleStateChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnDestroyed?.Invoke(this);
    }
    #endregion

    #region Public API
    /// <summary>
    /// Starts a fresh game: resets progress to 0, picks a new random map,
    /// and configures the elevators for the first round (exit only).
    /// Safe to call at any point to fully restart the loop.
    /// </summary>
    public void NewGame()
    {
        if (!isServer) return;

        CurrentProgress = 0;
        ElevatorCoolDown = false;
        hasPendingInteraction = false;

        StopPunishmentTimer();
        if (mapChangeCoroutine != null)
        {
            StopCoroutine(mapChangeCoroutine);
            mapChangeCoroutine = null;
        }

        PickStartingMap();
        SetElevatorInteraction(entryEnabled: false, exitEnabled: true); // unique to first round.

        InvokeOnProgressChanged(CurrentProgress);
        InvokeOnGameReset();

        LogProgress("Game reset.");
    }

    public int CorrectDecisionsToWin() => requiredCorrectDecisions;

    /// <summary>
    /// Applies and clears any pending elevator interaction state.
    /// Wire this to each ElevatorExit's OnFullyOpened UnityEvent in the Inspector.
    /// The isServer guard ensures the queued state (which only exists on the server)
    /// is only ever consumed there.
    /// </summary>
    public void ApplyPendingElevatorInteraction()
    {
        if (!isServer || !hasPendingInteraction) return;

        hasPendingInteraction = false;
        SetElevatorInteraction(pendingEntryEnabled, pendingExitEnabled);
    }
    #endregion

    #region Event Subscribers
    /// <summary>
    /// Reacts to every room state change reported by <see cref="AnomalyManager"/>.
    /// Owns all elevator configuration and game event firing so state-driven
    /// side effects are handled in one place rather than scattered across methods.
    /// </summary>
    private void HandleStateChanged(AnomalyManager.RoomState newState)
    {
        if (!isServer) return;

        switch (newState)
        {
            case AnomalyManager.RoomState.NormalRoom:
                bool entryEnabled = !isFirstRoom;
                QueueElevatorInteraction(entryEnabled, exitEnabled: true);
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: false);
                break;

            case AnomalyManager.RoomState.AnomalyRoom:
                QueueElevatorInteraction(entryEnabled: true, exitEnabled: true);
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: false);
                break;

            case AnomalyManager.RoomState.PunishmentRoom:
                HandlePunishmentRoomEntry();
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: false);
                break;

            case AnomalyManager.RoomState.WinRoom:
                QueueElevatorInteraction(entryEnabled: true, exitEnabled: true);
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: false);
                InvokeOnGameWon();
                LogProgress("Game won! Use the entry elevator to play again, or use the exit elevator to return to the Lobby!");
                break;
        }
    }

    /// <summary>
    /// Fired by <see cref="MapOrientor"/> whenever either elevator is interacted with.
    /// Immediately locks the used elevator and waits <see cref="mapChangeDelay"/> seconds
    /// before routing to the correct handler based on the current room state.
    /// </summary>
    private void HandleElevatorInteracted(LevelExitPoint usedElevator, bool decision)
    {
        if (!isServer || !TryStartCooldown()) return;

        usedElevator.SetInteraction(false);
        mapChangeCoroutine = StartCoroutine(PerformMapChange());

        StopPunishmentTimer();

        IEnumerator PerformMapChange()
        {
            yield return new WaitForSeconds(mapChangeDelay);

            AnomalyManager.RoomState currentState = anomalyManager.CurrentState;

            switch (currentState)
            {
                case AnomalyManager.RoomState.NormalRoom:
                case AnomalyManager.RoomState.AnomalyRoom:
                    HandleCoreLoopDecision(decision);
                    break;

                case AnomalyManager.RoomState.PunishmentRoom:
                    HandlePunishmentRoomExit();
                    break;

                case AnomalyManager.RoomState.WinRoom:
                    HandleWinRoomExit(decision);
                    break;
            }

            mapChangeCoroutine = null;
            TryStartCooldown();
        }
    }
    #endregion

    #region Core Loop Handling
    /// <summary>
    /// Evaluates the player's elevator choice against the actual room state
    /// and dispatches to <see cref="HandleCorrectDecision"/> or <see cref="HandleWrongDecision"/>.
    /// </summary>
    private void HandleCoreLoopDecision(bool decision)
    {
        bool decisionIsCorrect = decision == anomalyManager.HasAnomaly;
        Debug.Log($"[GameManager] Used entry: {decision} | HasAnomaly: {anomalyManager.HasAnomaly} | Correct: {decisionIsCorrect}");

        if (decisionIsCorrect) HandleCorrectDecision();
        else HandleWrongDecision();
    }

    /// <summary>
    /// Increments progress and either triggers the win condition or
    /// instructs <see cref="AnomalyManager"/> to decide the next map variation.
    /// </summary>
    private void HandleCorrectDecision()
    {
        CurrentProgress++;
        InvokeOnProgressChanged(CurrentProgress);

        LogProgress($"Correct!");

        if (CurrentProgress >= requiredCorrectDecisions)
        {
            anomalyManager.EnableWinRoom_Server();
            return;
        }

        // Decide the next map variation - random, honours anomalyChance.
        // MapOrientor.OrientMap is already subscribed to AnomalyManager.OnMapChanged,
        // so the new map will be positioned correctly relative to the elevators automatically.

        if (CurrentProgress == 1) anomalyManager.DecideNextMapVariation(true);
        else anomalyManager.DecideNextMapVariation();

        LogProgress($"Next variation decided. HasAnomaly: {anomalyManager.HasAnomaly}");
    }

    /// <summary>
    /// Instructs <see cref="AnomalyManager"/> to enable a punishment room.
    /// Elevator configuration is handled reactively via <see cref="HandleStateChanged"/>.
    /// </summary>
    private void HandleWrongDecision() => anomalyManager.EnablePunishmentRoom_Server();

    /// <summary>
    /// Handles the logic for entering the punishment room after a wrong decision is made.
    /// Applied immediately rather than deferred — the room has already loaded.
    /// </summary>
    private void HandlePunishmentRoomEntry()
    {
        SetElevatorInteraction(entryEnabled: false, exitEnabled: true);

        //BeginPunishmentRoomTimer_ObserversRpc(punishmentTimeLimit);

        InvokeOnWrongDecision();
        LogProgress($"Wrong decision — punishment room. Use the exit elevator to resume." +
            $"{punishmentTimeLimit}s to reach the exit elevator.");
    }

    /// <summary>
    /// Called when the player uses the exit elevator inside the punishment room.
    /// Resumes the core loop with progress saved, picking a new map variation.
    /// </summary>
    private void HandlePunishmentRoomExit()
    {
        if (isServer) anomalyManager.DecideNextMapVariation();
        LogProgress("Resuming from punishment room.");
    }

    /// <summary>
    /// Handles the player's choice upon leaving the win room.
    /// Routes to <see cref="HandleWinRoomReplay"/> if the entry elevator was used,
    /// or <see cref="HandleWinRoomReturnToLobby"/> if the exit elevator was used.
    /// </summary>
    private void HandleWinRoomExit(bool decision)
    {
        if (decision) HandleWinRoomReplay();
        else HandleWinRoomReturnToLobby();
    }
    #endregion

    #region Win Room Logic
    /// <summary>
    /// Called when the player uses the entry elevator in the win room.
    /// Resets the game entirely back to progress 0 and starts the loop again.
    /// </summary>
    private void HandleWinRoomReplay()
    {
        LogProgress("Returning from Win room. Resetting progress to 0.");
        NewGame();
    }

    /// <summary>
    /// Called when the player uses the exit elevator in the win room.
    /// Returns all players to the lobby via <see cref="SessionManager.RequestReturnToLobby"/>.
    /// </summary>
    private void HandleWinRoomReturnToLobby()
    {
        LogProgress("Returning from Win Room - returning to lobby.");
        SessionManager.Instance.RequestReturnToLobby();
    }
    #endregion

    #region Punishment Timer
    public void StartPunishTimer_Server()
    {
        if (!isServer || anomalyManager.CurrentState != AnomalyManager.RoomState.PunishmentRoom) return;
        BeginPunishmentRoomTimer_ObserversRpc(punishmentTimeLimit);
    }

    [ObserversRpc]
    private void BeginPunishmentRoomTimer_ObserversRpc(float timeLimit)
    {
        if (punishmentTimerCoroutine != null)
        {
            Debug.LogWarning("[GameManager] Punishment timer already running. Restarting with new time limit.");
            StopCoroutine(punishmentTimerCoroutine);
        }

        punishmentTimerCoroutine = StartCoroutine(PunishmentTimer(timeLimit));
    }

    private IEnumerator PunishmentTimer(float timeLimit)
    {
        float timeRemaining = timeLimit;

        while (timeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            timeRemaining -= 1f;
            OnPunishmentTimerTick.Invoke(timeRemaining);
            Debug.Log($"[GameManager] Punishment Room - {timeRemaining}s remaining");
        }

        LogProgress("Punishment timer expired - resetting progress to 0");

        if (isServer)
        {
            InvokeOnPunishmentTimerExpired();
            SessionManager.Instance.RequestReturnToLobby();
        }
    }

    [ObserversRpc]
    private void StopPunishmentTimer()
    {
        if (punishmentTimerCoroutine == null) return;

        StopCoroutine(punishmentTimerCoroutine);
        punishmentTimerCoroutine = null;
    }

    [ObserversRpc] private void InvokeOnPunishmentTimerExpired() => OnPunishmentTimerExpired?.Invoke();
    #endregion

    #region Elevator Control
    /// <summary>
    /// Stores the desired elevator interaction state to be applied once the doors
    /// finish their open animation. Consumed by <see cref="ApplyPendingElevatorInteraction"/>,
    /// which is wired in the Inspector to each ElevatorExit's OnFullyOpened UnityEvent.
    /// </summary>
    private void QueueElevatorInteraction(bool entryEnabled, bool exitEnabled)
    {
        pendingEntryEnabled = entryEnabled;
        pendingExitEnabled = exitEnabled;
        hasPendingInteraction = true;
    }

    /// <summary>
    /// Sets the interactability of both elevators independently.
    /// </summary>
    private void SetElevatorInteraction(bool entryEnabled, bool exitEnabled)
    {
        mapOrientor.EntryElevator.SetInteraction(entryEnabled);
        mapOrientor.ExitElevator.SetInteraction(exitEnabled);
    }

    /// <summary>
    /// Stamps the anomaly decision value onto each elevator.
    /// Must be called before elevators are opened so they fire the correct value on interact.
    /// </summary>
    private void SetElevatorChoice(bool entryHasAnomaly, bool exitHasAnomaly)
    {
        mapOrientor.EntryElevator.SetChoice(entryHasAnomaly);
        mapOrientor.ExitElevator.SetChoice(exitHasAnomaly);
    }
    #endregion

    #region Helpers

    private void PickStartingMap()
    {
        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
        {
            int selectedIndex = SessionManager.Instance.CurrentSession.SelectedLevelMapIndex;
            anomalyManager.PickMapByIndex_Server(selectedIndex);
            return;
        }
        
        // fallback case
        anomalyManager.PickMap_Server();
    }
    
    private bool TryStartCooldown()
    {
        if (ElevatorCoolDown) return false;

        ElevatorCoolDown = true;
        Invoke(nameof(ResetCooldown), decisionCooldown);
        return true;
    }

    private void ResetCooldown() => ElevatorCoolDown = false;

    private void LogProgress(string context) => Debug.Log($"[GameManager] {context} | Progress: {CurrentProgress} / {requiredCorrectDecisions}");

    private void ValidateDependencies()
    {
        if (anomalyManager == null)
            Debug.LogError("[GameManager] AnomalyManager reference is missing! Assign it in the Inspector.");

        if (mapOrientor == null)
            Debug.LogError("[GameManager] MapOrientor reference is missing! Assign it in the Inspector.");

        if (requiredCorrectDecisions <= 0)
            Debug.LogWarning("[GameManager] Required correct decisions is 0 or less — the game will end immediately.");
    }
    #endregion
}