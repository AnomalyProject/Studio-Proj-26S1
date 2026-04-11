using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class LevelExitPoint : NetworkBehaviour, IInteractable<PlayerBody>
{
    [Header("Settings")] Collider col;

    // Checks
    [SerializeField] bool noNetworkTesting = false;
    [SerializeField] private SyncVar<bool> bHasAnomaly = new(ownerAuth: false);
    private SyncVar<bool> bIsAvailable = new SyncVar<bool>(ownerAuth: false);
    private SyncHashSet<NetworkID> playersInArea = new SyncHashSet<NetworkID>(ownerAuth: false);

    // Events
    public UnityEvent<bool> OnActivateExit;

    /// <summary>
    /// Fires when an exit point is interacted with and the corresponding decision (Has Anomaly or not).
    /// </summary>
    public Action<LevelExitPoint, bool> OnExitActivated;

    void Awake()
    {
        if(!GetComponentInChildren<ExitInteractable>())
        {
            Debug.LogError($"{gameObject.name}: No ExitInteractable found as a child. Please add one for interaction.");
        }

        col = GetComponent<Collider>();
        col.isTrigger = true;
        bIsAvailable.value = true;
    }
    public bool CanInteract(PlayerBody interactor) => HasEnoughPlayers() && bIsAvailable.value;

    public bool TryInteract(PlayerBody interactor)
    {
        Debug.Log("Interacted with exit");
        Exit();
        return true;
    }

    #region Exit
    /// <summary>
    /// notifies the game with an event weather there is an anomaly or not.
    /// </summary>
    [ObserversRpc(requireServer: false)] private void Exit()
    {
        Debug.Log($"Exit Activated. Anomaly Presence: {bHasAnomaly}");
        OnActivateExit?.Invoke(bHasAnomaly);
        OnExitActivated?.Invoke(this, bHasAnomaly);
    }
    #endregion

    #region Triggers
    /// <summary>
    /// Checks if player(uses layer mask for player detection) is in the trigger volume.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out PlayerBody player) && player.id.HasValue)
        {
            playersInArea.Add(player.id.Value);
        }
    }

    // See OnTriggerEnter summary.
    private void OnTriggerExit(Collider other)
    {
        if(!isServer) return;

        if (other.TryGetComponent(out PlayerBody player) && player.id.HasValue)
        {
            playersInArea.Remove(player.id.Value);
        }
    }
    #endregion

    #region Player in Area Count
    /// <summary>
    /// Checks if all players are in the area.
    /// </summary>
    private bool HasEnoughPlayers()
    {
        if (noNetworkTesting)
        {
            Debug.Log($"[FAKE MODE] Players in Area: {playersInArea.Count}/1. Can Interact: {CanInteract(null)}");
           

            Debug.Log($"Players in Area: {playersInArea.Count}/{1}. Can Interact: {CanInteract(null)}");
            return playersInArea.Count >= 1; ;
        }

        Debug.Log($"Player In Area: {playersInArea.Count} | Players in Session: {NetworkManager.main.playerCount}");
        return playersInArea.Count >= NetworkManager.main.playerCount;
    }
#endregion

    #region Interactable Collider
    /// <summary>
    /// Enables/Disables the interaction mode.
    /// </summary>
    /// <param name="active"></param>
    [ServerRpc] public void SetInteraction(bool active) => bIsAvailable.value = active;
    [ServerRpc] public void SetChoice(bool hasAnomaly) => bHasAnomaly.value = hasAnomaly;
    #endregion
}