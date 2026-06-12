using System.Collections;
using UnityEngine.Events;
using UnityEngine;
using PurrNet;

public class TutorialManager : NetworkBehaviour
{
    [SerializeField] private AnomalyMap mainLevel;
    [SerializeField] private AnomalyMap voidLevel;
    private AnomalyMap activeLevel;
    private MapOrientor mapOrientor;

    [Header("Decision Cooldown")]
    [Tooltip("Seconds after a decision is made before another can be registered. Prevents spam.")]
    [SerializeField, Min(1)] private float decisionCooldown = 2f;

    [Tooltip("Seconds between elevator interaction and map change. Allows time for animations, feedback, etc.")]
    [SerializeField, Min(.5f)] private float elevatorSetupDelay = 2;

    [Tooltip("Seconds the player has to exit the punishment room before progress resets.")]
    [SerializeField, Min(1f)] private float voidTimeLimit = 3600;

    [SerializeField] private int CurrentProgress = 1;
    private bool ElevatorCoolDown;

    private Coroutine voidTimerCoroutine;

    public UnityEvent<float> OnVoidTimerTick;
    public UnityEvent OnWrongDecisionBeforeVoid, OnRightDecisionBeforeVoid, OnVoidTimerExpired;

    private void Awake() => mapOrientor = GetComponent<MapOrientor>();
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
    public void OnVoidPasscodeEntered() => ((ElevatorExit)mapOrientor.ExitElevator).OpenDoors();
    private void HandleElevatorButtonPressed(LevelExitPoint usedElevator, bool votedAnomalyPresent)
    {
        if (!TryStartCooldown()) return;
        usedElevator.SetInteraction(false);
        StartCoroutine(ElevatorSetup(votedAnomalyPresent));
        StopVoidTimer();
    }
    private IEnumerator ElevatorSetup(bool votedAnomalyPresent)
    {
        yield return new WaitForSeconds(elevatorSetupDelay);

        switch (CurrentProgress)
        {
            case 1: // First anomaly (forced correct decision)
                QueueElevatorInteraction(entryEnabled: true, exitEnabled: false);
                break;
            default: // No anomaly
                QueueElevatorInteraction(entryEnabled: false, exitEnabled: true);
                break;
            case 3: // Second anomaly (player can vote incorrectly)
                QueueElevatorInteraction(entryEnabled: true, exitEnabled: true);
                break;
            case 4: // Entering Void (vending machine tutorial)
                SetElevatorInteraction(entryEnabled: false, exitEnabled: true);
                EnvironmentLightingManager.Instance?.SetEnvironmentLighting(1);
                ((ElevatorExit)mapOrientor.ExitElevator).CloseDoors();
                if (votedAnomalyPresent)
                {
                    // Play narration "Excellent work! But, for the sake our training exercise, here's what would have happened of you entered the wrong elevator."
                    OnRightDecisionBeforeVoid?.Invoke();
                }
                else
                {
                    // Play narration "Good thing your contract doesn't cover vision insurance."
                    OnWrongDecisionBeforeVoid?.Invoke();
                }
                break;
            case 5: // Returning from void (collectibe tutorial)
                QueueElevatorInteraction(entryEnabled: false, exitEnabled: true);
                EnvironmentLightingManager.Instance?.SetEnvironmentLighting(0);
                break;
        }
        TryStartCooldown();
    }
    public void UpdateMap()
    {
        activeLevel.DisableAll(keepBase: true);
        mapOrientor.OrientMap(activeLevel);

        switch (CurrentProgress++)
        {
            case 1: // First anomaly (forced correct decision)
                mainLevel.AnomalyVariations[0].GroupRoot.SetActive(true);
                break;
            case 3: // Second anomaly (player can vote incorrectly)
                mainLevel.AnomalyVariations[1].GroupRoot.SetActive(true);
                break;
            case 4: // Void (vending machine tutorial)
                if (activeLevel == voidLevel) break;
                mainLevel.gameObject.SetActive(false);
                voidLevel.gameObject.SetActive(true);
                activeLevel = voidLevel;
                mapOrientor.OrientMap(activeLevel);
                CurrentProgress--; // The other elevator closing also increases the progress
                break;
            case 5: // Return from void (collectibe tutorial)
                voidLevel.gameObject.SetActive(false);
                mainLevel.gameObject.SetActive(true);
                activeLevel = mainLevel;
                mapOrientor.OrientMap(activeLevel);
                break;
            case 6: // Return to main menu
                StartCoroutine(TransitionToMenu());
                break;
        }

        mapOrientor.EntryElevator.SetChoice(true);
        mapOrientor.ExitElevator.SetChoice(false);
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
        if (activeLevel != voidLevel) return;
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
        SessionManager.Instance.RequestReturnToLobby();
    }
    #endregion

    #region Cooldown
    private bool TryStartCooldown()
    {
        if (ElevatorCoolDown) return false;

        ElevatorCoolDown = true;
        Invoke(nameof(ResetCooldown), decisionCooldown);
        return true;
    }
    private void ResetCooldown() => ElevatorCoolDown = false;
    #endregion
}
