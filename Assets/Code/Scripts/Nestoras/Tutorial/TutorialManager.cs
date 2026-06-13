using PurrNet;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TutorialManager : NetworkBehaviour
{
    public static TutorialManager Instance;

    [SerializeField] private AnomalyMap mainLevel;
    [SerializeField] private AnomalyMap voidLevel;
    [SerializeField] private PickUpSpawner duckSpawner;
    [SerializeField] private PickUpSpawner stalkerSpawner;
    [SerializeField] private GameObject collectible;
    private AnomalyMap activeLevel;
    private MapOrientor mapOrientor;

    [Header("Decision Cooldown")]
    [Tooltip("Seconds after a decision is made before another can be registered. Prevents spam.")]
    [SerializeField, Min(1)] private float decisionCooldown = 2f;

    [Tooltip("Seconds between elevator interaction and map change. Allows time for animations, feedback, etc.")]
    [SerializeField, Min(.5f)] private float elevatorSetupDelay = 2;

    [Tooltip("Seconds the player has to exit the punishment room before progress resets.")]
    [SerializeField, Min(1f)] private float voidTimeLimit = 3600;

    [SerializeField] private int CurrentProgress = 0;

    private Coroutine voidTimerCoroutine;
    private bool votedAnomalyPresent;
    private bool unlockedVoidExit;

    public static event Action<TutorialManager> OnInitialized, OnDestroyed;
    public UnityEvent<float> OnVoidTimerTick;
    public UnityEvent<string> onFloorChanged;
    public UnityEvent AfterFirstElevator, AfterSecondElevator, AfterEnteringVoid, OnVoidExitButtonPressed, LeavingVoid, OnWrongDecisionBeforeVoid, OnRightDecisionBeforeVoid, OnVoidTimerExpired;

    private void Awake()
    {
        Instance = this;
        OnInitialized?.Invoke(this);

        mapOrientor = GetComponent<MapOrientor>();
        mapOrientor.ExitElevator.QueueOnSpawned(() => ((ElevatorExit)mapOrientor.ExitElevator).CloseDoors());
    }
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        activeLevel = mainLevel;
        mapOrientor.OrientMap(mainLevel);
        activeLevel.gameObject.SetActive(true);
        SetElevatorInteraction(entryEnabled: false, exitEnabled: true);
    }
    private void OnEnable() => MapOrientor.OnElevatorInteracted += HandleElevatorButtonPressed;
    private void OnDisable() => MapOrientor.OnElevatorInteracted -= HandleElevatorButtonPressed;
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Instance = null;
        OnDestroyed?.Invoke(this);
    }
    public void OnTaskCompleted()
    {
        if (activeLevel == voidLevel) unlockedVoidExit = true;
        ((ElevatorExit)mapOrientor.ExitElevator).OpenDoors();
    }
    private void HandleElevatorButtonPressed(LevelExitPoint usedElevator, bool votedAnomalyPresent)
    {
        usedElevator.SetInteraction(false);
        StartCoroutine(ElevatorSetup(votedAnomalyPresent));
        StopVoidTimer();
    }
    // On Button Press
    private IEnumerator ElevatorSetup(bool votedAnomalyPresent)
    {
        yield return new WaitForSeconds(elevatorSetupDelay);
        this.votedAnomalyPresent = votedAnomalyPresent;

        switch (CurrentProgress++)
        {
            case 0: // First anomaly (forced correct decision)
                QueueElevatorInteraction(entryEnabled: true, exitEnabled: false);
                break;
            case 1: // No anomaly
                QueueElevatorInteraction(entryEnabled: false, exitEnabled: true);
                break;
            case 2: // Second anomaly (player can vote incorrectly)
                QueueElevatorInteraction(entryEnabled: true, exitEnabled: true);
                break;
            case 3: // Entering Void (vending machine tutorial)
                SetElevatorInteraction(entryEnabled: false, exitEnabled: true);
                ((ElevatorExit)mapOrientor.ExitElevator).CloseDoors(); // Also close other elevator (passcode needed for it to open again)
                EnvironmentLightingManager.Instance?.SetEnvironmentLighting(1);
                break;
            case 4: // Returning from void (collectibe tutorial)
                QueueElevatorInteraction(entryEnabled: false, exitEnabled: true);
                ((ElevatorExit)mapOrientor.ExitElevator).CloseDoors(); // Also close other elevator (pick up collectible for it to open again)
                EnvironmentLightingManager.Instance?.SetEnvironmentLighting(0);
                OnVoidExitButtonPressed?.Invoke();
                break;
        }
    }
    // On Doors Fully Closed
    public void UpdateMap()
    {
        activeLevel.DisableAll(keepBase: true);
        mapOrientor.OrientMap(activeLevel);

        switch (CurrentProgress)
        {
            case 1: // First anomaly (forced correct decision)
                mainLevel.AnomalyVariations[0].GroupRoot.SetActive(true);
                break;
            case 3: // Second anomaly (player can vote incorrectly)
                mainLevel.AnomalyVariations[1].GroupRoot.SetActive(true);
                break;
            case 4: // Void (vending machine tutorial)
                if (votedAnomalyPresent) OnRightDecisionBeforeVoid?.Invoke();
                else OnWrongDecisionBeforeVoid?.Invoke();
                SwichLevel(voidLevel);
                duckSpawner.SpawnItems();
                stalkerSpawner.SpawnItems();
                break;
            case 5: // Return from void (collectibe tutorial)
                LeavingVoid?.Invoke();
                SwichLevel(mainLevel);
                collectible.SetActive(true);
                break;
            case 6: // Return to main menu
                StartCoroutine(TransitionToMenu());
                break;
        }

        mapOrientor.EntryElevator.SetChoice(true);
        mapOrientor.ExitElevator.SetChoice(false);
    }
    // On Doors Open Start
    public void UpdateFloorNumbers()
    {
        switch (CurrentProgress)
        {
            case 1: // First anomaly (forced correct decision)
                onFloorChanged.Invoke("1/4");
                break;
            case 2: // No Anomaly
                onFloorChanged.Invoke("2/4");
                break;
            case 3: // Second anomaly (player can vote incorrectly)
                onFloorChanged.Invoke("3/4");
                break;
            case 5:
                onFloorChanged.Invoke("4/4");
                break;
        }
    }
    // On Doors Fully Opened
    public void Narrate()
    {
        switch (CurrentProgress)
        {
            case 1: // First anomaly (forced correct decision)
                AfterFirstElevator?.Invoke();
                break;
            case 2: // No Anomaly
                AfterSecondElevator?.Invoke();
                break;
            case 3: // Second anomaly (player can vote incorrectly)
                break;
            case 4: // Void (vending machine tutorial)
                if (!unlockedVoidExit) AfterEnteringVoid?.Invoke();
                break;
        }
    }
    private void StopVoidTimer()
    {
        if (voidTimerCoroutine == null) return;
        StopCoroutine(voidTimerCoroutine);
        voidTimerCoroutine = null;
    }
    private IEnumerator TransitionToMenu()
    {
        BlackFadeManager.Instance?.FadeIn();
        yield return new WaitForSeconds(BlackFadeManager.Instance.TransitionTime);
        SessionModeManager.Instance.ReturnToMenu();
    }
    public void SetCurrentProgress(int progress) => CurrentProgress = progress;
    private void SwichLevel(AnomalyMap newLevel)
    {
        activeLevel.gameObject.SetActive(false);
        activeLevel = newLevel;
        activeLevel.gameObject.SetActive(true);
        mapOrientor.OrientMap(activeLevel);
    }

    #region Elevator Control
    // Pending elevator interaction state, applied once the doors finish animating.
    // Set on the server via QueueElevatorInteraction; consumed on the server via
    // ApplyPendingElevatorInteraction, which is wired in the Inspector to each
    // ElevatorExit's OnFullyOpened UnityEvent.
    private bool pendingEntryEnabled;
    private bool pendingExitEnabled;
    private bool hasPendingInteraction;

    /// <summary>
    /// Applies and clears any pending elevator interaction state.
    /// Wire this to each ElevatorExit's OnFullyOpened UnityEvent in the Inspector.
    /// The isServer guard ensures the queued state (which only exists on the server)
    /// is only ever consumed there.
    /// </summary>
    public void ApplyPendingElevatorInteraction()
    {
        if (!hasPendingInteraction) return;
        hasPendingInteraction = false;
        SetElevatorInteraction(pendingEntryEnabled, pendingExitEnabled);
        Narrate();
    }

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
    #endregion

    #region Void Timer
    public void StartVoidTimer()
    {
        if (activeLevel != voidLevel || unlockedVoidExit) return;
        if (voidTimerCoroutine != null) StopCoroutine(voidTimerCoroutine);
        voidTimerCoroutine = StartCoroutine(VoidTimer(voidTimeLimit));
    }
    private IEnumerator VoidTimer(float timeLimit)
    {
        float timeRemaining = timeLimit;

        while (timeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            timeRemaining -= 1f;
            OnVoidTimerTick.Invoke(timeRemaining);
        }
        OnVoidTimerExpired?.Invoke();
    }
    #endregion
}
