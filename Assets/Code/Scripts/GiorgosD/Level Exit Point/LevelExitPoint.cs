using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class LevelExitPoint : MonoBehaviour, IInteractable<FPSController>
{
    [Header("Settings")]
    [SerializeField] private LayerMask playerMask;
    Collider col;

    // Checks
    private bool bEnoughPlayers, bIsAvailable;
    [SerializeField] private bool bHasAnomaly;

    private HashSet<FPSController> playersInArea = new HashSet<FPSController>();

    // Events
    public UnityEvent<bool> OnActivateExit;

    void Awake()
    {
        if(!GetComponentInChildren<ExitInteractable>())
        {
            Debug.LogError($"{gameObject.name}: No ExitInteractable found as a child. Please add one for interaction.");
        }

        col = GetComponent<Collider>();
        col.isTrigger = true;
        bIsAvailable = true;
    }
    public bool CanInteract(FPSController interactor) => bEnoughPlayers && bIsAvailable;

    public bool TryInteract(FPSController interactor)
    {
        Exit();
        return true;
    }

    #region Exit
    /// <summary>
    /// notifies the game with an event weather there is an anomaly or not.
    /// </summary>
    private void Exit()
    {
        Debug.Log($"Exit Activated. Anomaly Presence: {bHasAnomaly}");
        OnActivateExit?.Invoke(bHasAnomaly);
    }
    #endregion

    #region Triggers
    /// <summary>
    /// Checks if player(uses layer mask for player detection) is in the trigger volume.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if(((1 << other.gameObject.layer) & playerMask) != 0)
        {
            if (other.TryGetComponent(out FPSController player))
            {
                playersInArea.Add(player);

                CheckPlayersInArea();
            }
        }
    }

    // See OnTriggerEnter summary.
    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerMask) != 0)
        {
            if (other.TryGetComponent(out FPSController player))
            {
                playersInArea.Remove(player);

                CheckPlayersInArea();
            }
        }
    }
    #endregion

    #region Player in Area Count
    /// <summary>
    /// Checks if all players are in the area.
    /// </summary>
    private void CheckPlayersInArea()
    {
#if UNITY_EDITOR == false
        // Actual version
        bEnoughPlayers = playersInArea.Count >= SessionManager.Instance.CurrentSession.Players.Count;

        Debug.Log($"Players in Area: {playersInArea.Count}/{SessionManager.Instance.CurrentSession.Players.Count}. Can Interact: {CanInteract(null)}");
#else
        //for testing purposes only
        bEnoughPlayers = playersInArea.Count >= 1;

        Debug.Log($"Players in Area: {playersInArea.Count}/{1}. Can Interact: {CanInteract(null)}");
    #endif
    }
#endregion

    #region Interactable Collider
    /// <summary>
    /// Enables/Disables the interaction mode.
    /// </summary>
    /// <param name="active"></param>
    public void SetInteraction(bool active) => bIsAvailable = active;
    #endregion
}