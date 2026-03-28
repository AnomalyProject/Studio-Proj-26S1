using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelExitPoint : MonoBehaviour, IInteractable<FPSController>
{
    [Header("Settings")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private Collider interablecCollider;

    // Checks
    private bool bCanInteract;
    [SerializeField] private bool bHasAnomaly;

    private HashSet<FPSController> playersInArea = new HashSet<FPSController>();

    // Events
    public event Action<bool> OnActivateExit;

    public bool CanInteract(FPSController interactor) => bCanInteract;

    public bool TryInteract(FPSController interactor)
    {
        if (!bCanInteract)
        {
            return false;
        }

        Debug.Log($"Gay test");
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
        bCanInteract = playersInArea.Count >= SessionManager.Instance.CurrentSession.Players.Count;

        Debug.Log($"Players in Area: {playersInArea.Count}/{SessionManager.Instance.CurrentSession.Players.Count}. Can Interact: {bCanInteract}");
    #else
        //for testing purposes only
        bCanInteract = playersInArea.Count >= 1;

        Debug.Log($"Players in Area: {playersInArea.Count}/{1}. Can Interact: {bCanInteract}");
    #endif
    }
#endregion

    #region Interactable Collider
    /// <summary>
    /// Enables/Desables the collider of the interactable.
    /// </summary>
    /// <param name="state"></param>
    public void SetCollider(bool state)
    {
        if (interablecCollider != null)
        {
            interablecCollider.enabled = state;
        }
    }
    #endregion
}