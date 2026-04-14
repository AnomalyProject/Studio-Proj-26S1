using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Single source of truth for the anomaly game flow.
/// Plain MonoBehaviour for single-player / local testing.
/// Listens to <see cref="MapOrientor.OnElevatorInteracted"/> for player decisions,
/// and to <see cref="AnomalyManager.OnStateChanged"/> to react to room transitions.
///
/// Correct decisions:
///   - Anomaly present  → player takes the ENTRY elevator (goes back)
///   - No anomaly       → player takes the EXIT elevator (continues forward)
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Inspector 
    [Header("Dependencies")]
    [SerializeField] AnomalyManager anomalyManager;
    [SerializeField] MapOrientor mapOrientor;

    [Header("Win Condition")]
    [Tooltip("How many correct decisions in a row are required to win.")]
    [SerializeField] int requiredCorrectDecisions = 5;

    [Header("Decision Cooldown")]
    [Tooltip("Seconds after a decision is made before another can be registered. Prevents spam.")]
    [SerializeField, Min(1)] float decisionCooldown = 2f;
    [SerializeField, Min(.5f)] float mapChangeDelay = 2;
    #endregion

    #region State
    public int CurrentProgress { get; private set; }
    bool IsOnCoolDown;
    #endregion

    #region Events
    // Events
    public UnityEvent<int> OnProgressChanged;
    public UnityEvent OnWrongDecision;
    public UnityEvent OnGameWon;
    public UnityEvent OnGameReset;
    #endregion

    #region Unity Lifecycle
    void Awake() => ValidateDependencies();
    void Start() => NewGame();

    void OnEnable()
    {
        MapOrientor.OnElevatorInteracted += HandleElevatorInteracted;
        anomalyManager.OnStateChanged += HandleStateChanged;
    }
    void OnDisable()
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
        CurrentProgress = 0;
        IsOnCoolDown = false;
        CancelInvoke(nameof(ResetCooldown));

        anomalyManager.PickMap_Server();
        SetElevatorInteraction(entryEnabled: false, exitEnabled: true); // unique to first round.

        OnProgressChanged?.Invoke(CurrentProgress);
        OnGameReset?.Invoke();

        LogProgress("Game reset.");
    }
    #endregion

    #region Event Subscribers
    /// <summary>
    /// Reacts to every room state change reported by <see cref="AnomalyManager"/>.
    /// Owns all elevator configuration and game event firing so state-driven
    /// side effects are handled in one place rather than scattered across methods.
    /// </summary>
    void HandleStateChanged(AnomalyManager.RoomState newState)
    {
        switch(newState)
        {
            case AnomalyManager.RoomState.NormalRoom:
            case AnomalyManager.RoomState.AnomalyRoom:
                SetElevatorInteraction(entryEnabled: true, exitEnabled: true);
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: false);
                break;

            case AnomalyManager.RoomState.PunishmentRoom:
                SetElevatorInteraction(entryEnabled: false, exitEnabled: true);
                OnWrongDecision?.Invoke();
                LogProgress("Wrong decision — punishment room. Use the exit elevator to resume");
                break;
            case AnomalyManager.RoomState.WinRoom:
                SetElevatorInteraction(entryEnabled: true, exitEnabled: false);
                OnGameWon?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Fired by <see cref="MapOrientor"/> whenever either elevator is interacted with.
    /// Immediately locks the used elevator and waits <see cref="mapChangeDelay"/> seconds
    /// before routing to the correct handler based on the current room state.
    /// </summary>
    void HandleElevatorInteracted(LevelExitPoint usedElevator, bool decision)
    {
        if (!TryStartCooldown()) return;

        usedElevator.SetInteraction(false);
        StartCoroutine(PerformMapChange());

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

            TryStartCooldown();
        }
    }
    #endregion

    #region Core Loop Handling
    /// <summary>
    /// Evaluates the player's elevator choice against the actual room state
    /// and dispatches to <see cref="HandleCorrectDecision"/> or <see cref="HandleWrongDecision"/>.
    /// </summary>
    void HandleCoreLoopDecision(bool decision)
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
    void HandleCorrectDecision()
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
    void HandleWrongDecision()
    {
        anomalyManager.EnablePunishmentRoom_Server();
        LogProgress("Wrong decision — punishment room enabled. Use the exit elevator to resume.");
    }

    /// <summary>
    /// Called when the player uses the exit elevator inside the punishment room.
    /// Resumes the core loop with progress saved, picking a new map variation.
    /// </summary>
    void HandlePunishmentRoomExit()
    {
        anomalyManager.DecideNextMapVariation();

        LogProgress("Resuming from punishment room.");
    }

    /// <summary>
    /// Called when the player uses the entry elevator inside the win room.
    /// Resets the game entirely back to progress 0.
    /// </summary>
    void HandleWinRoomExit()
    {
        LogProgress("Returning from Win room. Resetting progress to 0.");
        NewGame();
    }
    #endregion

    #region Elevator Control
    /// <summary>
    /// Sets the interactability of both elevators independently.
    /// Use this whenever the game changes state to control player navigation.
    /// </summary>
    void SetElevatorInteraction(bool entryEnabled, bool exitEnabled)
    {
        mapOrientor.EntryElevator.SetInteraction(entryEnabled);
        mapOrientor.ExitElevator.SetInteraction(exitEnabled);
    }

    /// <summary>
    /// Stamps the anomaly decision value onto each elevator.
    /// Must be called before elevators are opened so they fire the correct value on interact.
    /// Entry elevator always represents "anomaly present", exit always represents "no anomaly".
    /// </summary>
    void SetElevatorChoice(bool entryHasAnomaly, bool exitHasAnomaly)
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
    bool TryStartCooldown()
    {
        if (IsOnCoolDown) return false;

        IsOnCoolDown = true;
        Invoke(nameof(ResetCooldown), decisionCooldown);
        return true;
    }
    void ResetCooldown() => IsOnCoolDown = false;
    void LogProgress(string context) => Debug.Log($"[GameManager] {context} | Progress: {CurrentProgress} / {requiredCorrectDecisions}");
    void ValidateDependencies()
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