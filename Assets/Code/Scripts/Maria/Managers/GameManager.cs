using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Single source of truth for the anomaly game flow.
/// Plain MonoBehaviour for single-player / local testing.
/// Listens to MapOrientor.OnElevatorInteracted to know which elevator the player used,
/// then determines correctness and instructs AnomalyManager accordingly.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] AnomalyManager anomalyManager;
    [SerializeField] MapOrientor mapOrientor;

    [Header("Win Condition")]
    [Tooltip("How many correct decisions in a row are required to win.")]
    [SerializeField] int requiredCorrectDecisions = 5;

    [Header("Decision Cooldown")]
    [Tooltip("Seconds after a decision is made before another can be registered. Prevents spam.")]
    [SerializeField, Min(0f)] float decisionCooldown = 2f;

    [Header("Debug / Testing")]
    [Tooltip("When true, logs a fake punishment-room activation instead of requiring a real scene object.")]
    [SerializeField] bool fakePunishmentRoom = false;
    public int CurrentProgress { get; private set; }
    public bool IsGameActive => anomalyManager.CurrentState == AnomalyManager.RoomState.NormalRoom ||
                                anomalyManager.CurrentState == AnomalyManager.RoomState.AnomalyRoom;

    bool IsOnCoolDown;

    // Snapshot of the entry elevator taken at the start of each round, before any swap.
    LevelExitPoint cachedEntryElevator;


    // Events
    public UnityEvent<int> OnProgressChanged;
    public UnityEvent OnWrongDecision;
    public UnityEvent OnGameWon;
    public UnityEvent OnGameReset;

    void Awake() => ValidateDependencies();
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
    void Start() => InitialiseGame();

    /// <summary>
    /// Resets the game to its initial state: picks a fresh random map,
    /// clears progress, starts with no anomaly, and sends the player to spawn.
    /// </summary>
    public void ResetGame()
    {
        CurrentProgress = 0;
        IsOnCoolDown = false;
        CancelInvoke(nameof(ResetCooldown));

        anomalyManager.TryPickMap();
        anomalyManager.DecideNextMapVariation(false);

        cachedEntryElevator = mapOrientor.EntryElevator;
        SetElevatorInteraction(entryEnabled: false, exitEnabled: true);

        OnProgressChanged?.Invoke(CurrentProgress);
        OnGameReset?.Invoke();

        LogProgress("[GameManager] Game reset. Progress: 0. Waiting for player decision.");
    }

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
                SetElevatorChoice(entryHasAnomaly: true, exitHasAnomaly: true);
                OnWrongDecision?.Invoke();
                LogProgress("Wrong decision — punishment room. Use the exit elevator to resume");
                break;
            case AnomalyManager.RoomState.WinRoom:
                SetElevatorInteraction(entryEnabled: true, exitEnabled: false);
                OnGameWon?.Invoke();
                break;
        }
    }
    void HandleElevatorInteracted(LevelExitPoint usedElevator, bool decision)
    {
        if (IsOnCoolDown) return;

        IsOnCoolDown = true;
        Invoke(nameof(ResetCooldown), decisionCooldown);

        switch (anomalyManager.CurrentState)
        {
            case AnomalyManager.RoomState.NormalRoom:
            case AnomalyManager.RoomState.AnomalyRoom:
                HandleCoreLoopDecision(usedElevator);
                break;

            case AnomalyManager.RoomState.PunishmentRoom:
                HandlePunishmentRoomExit();
                break;

            case AnomalyManager.RoomState.WinRoom:
                HandleWinRoomExit();
                break;
        }
    }
    void HandleCoreLoopDecision(LevelExitPoint usedElevator)
    {
        bool usedEntryElevator = usedElevator == cachedEntryElevator;
        bool decisionIsCorrect = usedEntryElevator == anomalyManager.HasAnomaly;

        // Update cache now that MapOrientor has already swapped its references.
        cachedEntryElevator = mapOrientor.EntryElevator;

        Debug.Log($"[GameManager] Elevator: {usedElevator.name} | " +
                  $"Used entry: {usedEntryElevator} | HasAnomaly: {anomalyManager.HasAnomaly} | Correct: {decisionIsCorrect}");

        if (decisionIsCorrect) HandleCorrectDecision();
        else HandleWrongDecision();
    }
    void HandleCorrectDecision()
    {
        CurrentProgress++;
        OnProgressChanged?.Invoke(CurrentProgress);

        LogProgress($"[GameManager] Correct! Progress: {CurrentProgress}/{requiredCorrectDecisions}");

        if (CurrentProgress >= requiredCorrectDecisions)
        {
            anomalyManager.EnableWinRoom();
            return;
        }

        // Decide the next map variation - random, honours anomalyChance.
        // MapOrientor.OrientMap is already subscribed to AnomalyManager.OnMapChanged,
        // So new map will be positioned correctly relative to the elevators automatically.
        anomalyManager.DecideNextMapVariation();

        Debug.Log($"[GameManager] Next variation decided. HasAnomaly: {anomalyManager.HasAnomaly}");
    }

    void HandleWrongDecision()
    {
        if (fakePunishmentRoom)
        {
            HandleStateChanged(AnomalyManager.RoomState.PunishmentRoom);
            Debug.Log("[GameManager] FAKE punishment room activated (fakePunishmentRoom = true).");
            return;
        }

        anomalyManager.EnablePunishmentRoom();
        LogProgress("[GameManager] Wrong decision — punishment room enabled. Use the exit elevator to resume.");
    }

    void HandlePunishmentRoomExit()
    {
        anomalyManager.TryPickMap();
        anomalyManager.DecideNextMapVariation();

        cachedEntryElevator = mapOrientor.EntryElevator;

        LogProgress($"[GameManager] Resuming from punishment room. Progress kept at: {CurrentProgress}/{requiredCorrectDecisions}");
    }
    void HandleWinRoomExit()
    {
        LogProgress("Returning from Win room. Resetting progress to 0.");
        ResetGame();
    }
    void InitialiseGame() => ResetGame();
    void ResetCooldown() => IsOnCoolDown = false;
    void SetElevatorInteraction(bool entryEnabled, bool exitEnabled)
    {
        mapOrientor.EntryElevator.SetInteraction(entryEnabled);
        mapOrientor.ExitElevator.SetInteraction(exitEnabled);
    }
    void SetElevatorChoice(bool entryHasAnomaly, bool exitHasAnomaly)
    {
        mapOrientor.EntryElevator.SetChoice(entryHasAnomaly);
        mapOrientor.ExitElevator.SetChoice(exitHasAnomaly);
    }
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
}