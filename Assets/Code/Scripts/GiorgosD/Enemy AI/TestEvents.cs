using System;
using UnityEngine;

public class TestEvents : MonoBehaviour
{
    [SerializeField] private EnemyPawn pawn;

    private void Awake()
    {
        pawn.OnPlayerSpotted.AddListener(PlayerSpotted);
        pawn.OnPlayerAttacked.AddListener(PlayerAttacked);
    }

    private void PlayerSpotted(PlayerBody player)
    {
        if (player == null) Debug.LogWarning($"Failed to spot player data");
        Debug.LogWarning($"PlayerSpotted {player}");
    }

    private void PlayerAttacked(PlayerBody player)
    {
        if (player == null) Debug.LogWarning($"Failed to attack player data");
        Debug.LogWarning($"PlayerAttacked {player}");
    }

    private void OnDestroy()
    {
        pawn.OnPlayerSpotted.RemoveListener(PlayerSpotted);
        pawn.OnPlayerAttacked.RemoveListener(PlayerAttacked);
    }
}
