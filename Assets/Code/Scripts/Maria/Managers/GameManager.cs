using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Single source of truth for the anomaly game flow.
/// Plain MonoBehaviour for single-player / local testing.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] AnomalyManager anomalyManager;

    [Tooltip("The transform players are sent back to after each correct decision.")]
    [SerializeField] Transform spawnPoint;

    [Tooltip("The player transform to teleport to the spawn point.")]
    [SerializeField] Transform playerTransform;

    [Tooltip("All level exit points players can interact with to submit their decision.")]
    [SerializeField] LevelExitPoint[] exitPoints;

    [Header("Win Condition")]
    [Tooltip("How many correct decisions in a row are required to win.")]
    [SerializeField] int requiredCorrectDecisions = 5;

    [Header("Debug / Testing")]
    [Tooltip("When true, logs a fake punishment-room activation instead of requiring a real scene object.")]
    [SerializeField] bool fakePunishmentRoom = false;

    // State
    public int CurrentProgress { get; private set; }
    public bool IsGameActive { get; private set; }

    // Events
    public UnityEvent<int> OnProgressChanged;
    public UnityEvent OnWrongDecision;
    public UnityEvent OnGameWon;
    public UnityEvent OnGameReset;

    void Awake() => ValidateDependencies();

    void Start()
    {
        foreach (LevelExitPoint exit in exitPoints)
        {
            if (exit != null)
                exit.OnActivateExit.AddListener(HandlePlayerDecision);
        }

        InitialiseGame();
    }

    void OnDestroy()
    {
        foreach (LevelExitPoint exit in exitPoints)
        {
            if (exit != null)
                exit.OnActivateExit.RemoveListener(HandlePlayerDecision);
        }
    }

    /// <summary>
    /// Resets the game to its initial state: picks a fresh random map,
    /// clears progress, starts with no anomaly, and sends the player to spawn.
    /// </summary>
    public void ResetGame()
    {
        CurrentProgress = 0;
        IsGameActive = true;

        anomalyManager.TryPickMap();
        anomalyManager.DecideNextMapVariation(false);

        // Open exits — bHasAnomaly on each exit is fixed in the Inspector (player's choice).
        SetExitsAvailable(true);

        TeleportPlayerToSpawn();

        OnProgressChanged?.Invoke(CurrentProgress);
        OnGameReset?.Invoke();

        Debug.Log("[GameManager] Game reset. Progress: 0. Waiting for player decision.");
    }

    void HandlePlayerDecision(bool playerReportsAnomaly)
    {
        if (!IsGameActive) return;

        bool decisionIsCorrect = playerReportsAnomaly == anomalyManager.HasAnomaly;

        Debug.Log($"[GameManager] Player decision: anomaly={playerReportsAnomaly} | " +
                  $"Actual anomaly={anomalyManager.HasAnomaly} | Correct={decisionIsCorrect}");

        SetExitsAvailable(false);

        if (decisionIsCorrect)
            HandleCorrectDecision();
        else
            HandleWrongDecision();
    }

    void HandleCorrectDecision()
    {
        CurrentProgress++;
        OnProgressChanged?.Invoke(CurrentProgress);

        Debug.Log($"[GameManager] Correct! Progress: {CurrentProgress}/{requiredCorrectDecisions}");

        if (CurrentProgress >= requiredCorrectDecisions)
        {
            TriggerWin();
            return;
        }

        // Decide the next map variation (random, honours anomalyChance).
        anomalyManager.DecideNextMapVariation();

        SetExitsAvailable(true);

        // Send the player back to the beginning of the level.
        TeleportPlayerToSpawn();
    }

    void HandleWrongDecision()
    {
        IsGameActive = false;
        OnWrongDecision?.Invoke();

        if (fakePunishmentRoom)
            Debug.Log("[GameManager] FAKE punishment room activated (fakePunishmentRoom = true).");
        else
            anomalyManager.EnablePunishmentRoom();

        TeleportPlayerToSpawn();

        Debug.Log("[GameManager] Wrong decision — punishment room enabled. Call ResetGame() to restart.");
    }

    void TriggerWin()
    {
        IsGameActive = false;
        anomalyManager.EnableWinRoom();
        OnGameWon?.Invoke();

        Debug.Log("[GameManager] Win condition met! Win room enabled.");
    }

    void InitialiseGame() => ResetGame();

    void TeleportPlayerToSpawn()
    {
        if (spawnPoint == null || playerTransform == null)
        {
            Debug.LogWarning("[GameManager] Spawn point or player transform not assigned — skipping teleport.");
            return;
        }

        playerTransform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"[GameManager] Teleported player to spawn: {spawnPoint.position}");
    }

    /// <summary>
    /// Enables or disables interaction on all registered exit points.
    /// </summary>
    void SetExitsAvailable(bool available)
    {
        foreach (var exit in exitPoints)
            exit?.SetInteraction(available);
    }

    void ValidateDependencies()
    {
        if (anomalyManager == null)
            Debug.LogError("[GameManager] AnomalyManager reference is missing! Assign it in the Inspector.");

        if (spawnPoint == null)
            Debug.LogWarning("[GameManager] No spawn point assigned. Players won't be teleported on correct decisions.");

        if (playerTransform == null)
            Debug.LogWarning("[GameManager] No player transform assigned. Players won't be teleported on correct decisions.");

        if (exitPoints == null || exitPoints.Length == 0)
            Debug.LogWarning("[GameManager] No LevelExitPoints assigned. Players won't be able to submit decisions.");
    }
}