using System.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine;
using PurrNet;
using System;

[RequireComponent(typeof(Collider))]
public class LevelExitPoint : NetworkBehaviour, IInteractable<PlayerBody>
{
    [Header("Settings")] Collider col;

    // Checks
    [SerializeField] protected SyncVar<bool> bHasAnomaly = new(ownerAuth: false);
    protected SyncVar<bool> bIsAvailable = new SyncVar<bool>(initialValue: true, ownerAuth: false);
    protected SyncHashSet<NetworkID> playersInArea = new SyncHashSet<NetworkID>(ownerAuth: false);

    // Events
    public UnityEvent<bool> OnActivateExit, OnPlayersChanged, OnAvailabilityChanged;

    /// <summary>
    /// Fires when an exit point is interacted with and the corresponding decision (Has Anomaly or not).
    /// </summary>
    public Action<LevelExitPoint, bool> OnExitActivated;
    public bool IsReadyToInteract => HasEnoughPlayers() && bIsAvailable.value;

    protected virtual void Awake()
    {
        if(!GetComponentInChildren<ExitInteractable>())
        {
            Debug.LogError($"{gameObject.name}: No ExitInteractable found as a child. Please add one for interaction.");
        }

        col = GetComponent<Collider>();
        col.isTrigger = true;
        playersInArea.onChanged += HandlePlayersChanged;
        bIsAvailable.onChanged += OnAvailabilityChanged.Invoke;
    }

    private void HandlePlayersChanged(SyncHashSetChange<NetworkID> change) => OnPlayersChanged.Invoke(IsReadyToInteract);
    public Task<bool> CanInteract(PlayerBody interactor) => Task.FromResult(IsReadyToInteract);
    public Task<bool> TryInteract(PlayerBody interactor)
    {
        Debug.Log("Interacted with exit");
        Exit();
        return Task.FromResult(true);
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
        if (other.TryGetComponent(out PlayerBody player) && player.id.HasValue)
        {
            if (isServer) playersInArea.Add(player.id.Value);
            if (player.isOwner) player.transform.SetParent(transform, true);
        }
        else if(other.TryGetComponent(out ItemPickup pickup) && pickup.isOwner)
        {
            pickup.transform.SetParent(transform, true);
        }
    }

    // See OnTriggerEnter summary.
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerBody player) && player.id.HasValue)
        {
            if (isServer) playersInArea.Remove(player.id.Value);
            if (player.isOwner) player.transform.SetParent(null, true);
        }
    }
    #endregion

    #region Player in Area Count
    /// <summary>
    /// Checks if all players are in the area.
    /// </summary>
    private bool HasEnoughPlayers()
    {
        //Debug.Log($"Player In Area: {playersInArea.Count} | Players in Session: {NetworkManager.main.playerCount}");
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