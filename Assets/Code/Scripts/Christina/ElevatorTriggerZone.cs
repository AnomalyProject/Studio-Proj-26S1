using System;
using UnityEngine;
using PurrNet;

public class ElevatorTriggerZone : NetworkBehaviour
{
    [SerializeField] private ElevatorLobbyManager manager;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (TryGetPlayerID(other, out PlayerID playerID))
            manager.PlayerEntered(playerID);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        if (TryGetPlayerID(other, out PlayerID playerID))
            manager.PlayerExited(playerID);
    }

    private bool TryGetPlayerID(Collider other, out PlayerID playerID)
    {
        playerID = default;

        PlayerBody player = other.GetComponentInParent<PlayerBody>();
        if (player == null) return false;
        if (!player.OwnerPlayerID.HasValue) return false;

        playerID = player.OwnerPlayerID.Value;
        return true;
    }
}
