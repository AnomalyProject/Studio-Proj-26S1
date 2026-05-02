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
    #region Inspector 
    [Header("Dependencies")]
    [SerializeField] AnomalyManager anomalyManager;
    [SerializeField] MapOrientor mapOrientor;

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
    #endregion

    #region Events
    // Events
    public UnityEvent<int> OnProgressChanged;
    public UnityEvent<float> OnPunishmentTimerTick;
    public UnityEvent OnWrongDecision, OnPunishmentTimerExpired;
    public UnityEvent OnGameWon;
    public UnityEvent OnGameReset;
    #endregion

    #region Unity Lifecycle
    private void Awake() => ValidateDependencies();
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

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
    #endregion

    #region Public API
    /// <summary>
    /// Starts a fresh game: resets progress to 0, picks a new random map,
    /// and configures the elevators for the first round (exit only).
    /// Safe to call at any point to fully restart the loop.
    /// </summary>
    public void NewGame()
    {
        if(!isServer) return;

        CurrentProgress = 0;
        ElevatorCoolDown = false;

        StopPunishmentTimer();
        if(mapChangeCoroutine != null)
        {
            StopCoroutine(mapChangeCoroutine);
            mapChangeCoroutine = null;
        }

        anomalyManager.PickMap_Server();
        SetElevatorInteraction(entryEnabled: false, exitEnabled: true); // unique to first round.

        OnProgressChanged?.Invoke(CurrentProgress);
        OnGameReset?.Invoke();

        LogProgress("Game reset.");
    }

    public int CorrectDecisionsToWin() => requiredCorrectDecisions;
    #endregion

    #region Event Subscribers
    /// <summary>
    /// Reacts to every room state change reported by <see cref="AnomalyManager"/>.
    /// Owns all elevator configuration and game event firing so state-driven
    /// side effects are handled in one place rather than scattered across methods.
    /// </summary>
    private void HandleStateChanged(AnomalyManager.RoomState newState)
    {
        if(!isServer) return;

        switch(newState)
        {
            case AnomalyManager.RoomState.NormalRoom:
                bool entryEnabled = !isFirstRoom;
                SetElevatorInteraction(entryEnabled, exitEnabled: true);
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: false);
                break;

            case AnomalyManager.RoomState.AnomalyRoom:
                SetElevatorInteraction(entryEnabled: true, exitEnabled: true);
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: false);
                break;

            case AnomalyManager.RoomState.PunishmentRoom:
                HandlePunishmentRoomEntry();
                break;

            case AnomalyManager.RoomState.WinRoom:
                SetElevatorInteraction(entryEnabled: true, exitEnabled: false);
                OnGameWon?.Invoke();
                LogProgress("Game won! Use the entry elevator to play again!");
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
                    HandleWinRoomExit();
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
        OnProgressChanged?.Invoke(CurrentProgress);

        LogProgress($"Correct!");

        if (CurrentProgress >= requiredCorrectDecisions)
        {
            anomalyManager.EnableWinRoom_Server();
            return;
        }

        // Decide the next map variation - random, honours anomalyChance.
        // MapOrientor.OrientMap is already subscribed to AnomalyManager.OnMapChanged,
        // So new map will be positioned correctly relative to the elevators automatically.
        
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
    /// </summary>
    /// <remarks>Disables elevator entry, enables elevator exit, and starts the punishment room timer.
    /// Triggers the wrong decision event and logs progress. Call this method when the user must be sent to the
    /// punishment room as a result of an incorrect action.</remarks>
    private void HandlePunishmentRoomEntry()
    {
        SetElevatorInteraction(entryEnabled: false, exitEnabled: true);
        BeginPunishmentRoomTimer(punishmentTimeLimit);

        OnWrongDecision?.Invoke();
        LogProgress($"Wrong decision — punishment room. Use the exit elevator to resume." +
            $"{punishmentTimeLimit}s to reach the exit elevator.");
    }

    /// <summary>
    /// Called when the player uses the exit elevator inside the punishment room.
    /// Resumes the core loop with progress saved, picking a new map variation.
    /// </summary>
    private void HandlePunishmentRoomExit()
    {
        StopPunishmentTimer();
        if(isServer) anomalyManager.DecideNextMapVariation();

        LogProgress("Resuming from punishment room.");
    }

    /// <summary>
    /// Called when the player uses the entry elevator inside the win room.
    /// Resets the game entirely back to progress 0.
    /// </summary>
    private void HandleWinRoomExit()
    {
        LogProgress("Returning from Win room. Resetting progress to 0.");
        NewGame();
    }
    #endregion

    #region Punishment Timer
    /// <summary>
    /// Starts or restarts the punishment room timer with the specified time limit.
    /// </summary>
    /// <remarks>If a punishment timer is already running, it will be stopped and restarted with the new time
    /// limit.</remarks>
    /// <param name="timeLimit">The duration, in seconds, for which the punishment room timer should run. Must be greater than zero.</param>
    [ObserversRpc] private void BeginPunishmentRoomTimer(float timeLimit)
    {
        if (punishmentTimerCoroutine != null)
        {
            Debug.LogWarning("[GameManager] Punishment timer already running. Restarting with new time limit.");
            StopCoroutine(punishmentTimerCoroutine);
        }

        punishmentTimerCoroutine = StartCoroutine(PunishmentTimer(timeLimit));
    }
    /// <summary>
    /// Runs a countdown timer for the punishment phase and resets game progress when the time limit expires.
    /// </summary>
    /// <remarks>This coroutine should be started using StartCoroutine in a Unity MonoBehaviour. When the
    /// timer completes, game progress is reset. The timer logs the remaining time at one-second intervals.</remarks>
    /// <param name="timeLimit">The duration, in seconds, for the punishment timer. Must be greater than zero.</param>
    /// <returns>An enumerator that yields once per second until the timer expires.</returns>
    private IEnumerator PunishmentTimer(float timeLimit)
    {
        float timeRemaining = timeLimit;

        while(timeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            timeRemaining -= 1f;
            OnPunishmentTimerTick.Invoke(timeRemaining);
            Debug.Log($"[GameManager] Punishment Room - {timeRemaining}s remaining");
        }

        LogProgress("Punishment timer expired - resetting progress to 0");
        if(isServer) InvokeOnPunishmentTimerExpired();
        NewGame();
    }
    /// <summary>
    /// Stops the currently running punishment timer, if one is active.
    /// </summary>
    /// <remarks>This method has no effect if no punishment timer is running. Intended to be called remotely
    /// on all observers in a networked environment.</remarks>
    [ObserversRpc] private void StopPunishmentTimer()
    {
        if(punishmentTimerCoroutine == null) return;

        StopCoroutine(punishmentTimerCoroutine);
        punishmentTimerCoroutine = null;
    }

    [ObserversRpc] private void InvokeOnPunishmentTimerExpired() => OnPunishmentTimerExpired?.Invoke();
    #endregion

    #region Elevator Control
    /// <summary>
    /// Sets the interactability of both elevators independently.
    /// Use this whenever the game changes state to control player navigation.
    /// </summary>
    private void SetElevatorInteraction(bool entryEnabled, bool exitEnabled)
    {
        mapOrientor.EntryElevator.SetInteraction(entryEnabled);
        mapOrientor.ExitElevator.SetInteraction(exitEnabled);
    }

    /// <summary>
    /// Stamps the anomaly decision value onto each elevator.
    /// Must be called before elevators are opened so they fire the correct value on interact.
    /// Entry elevator always represents "anomaly present", exit always represents "no anomaly".
    /// </summary>
    private void SetElevatorChoice(bool entryHasAnomaly, bool exitHasAnomaly)
    {
        mapOrientor.EntryElevator.SetChoice(entryHasAnomaly);
        mapOrientor.ExitElevator.SetChoice(exitHasAnomaly);
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Attempts to start the decision cooldown. Returns false if already on cooldown,
    /// preventing duplicate interactions from being processed.
    /// </summary>
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

        if(requiredCorrectDecisions <= 0)
            Debug.LogWarning("[GameManager] Required correct decisions is 0 or less — the game will end immediately.");
    }
    #endregion
}