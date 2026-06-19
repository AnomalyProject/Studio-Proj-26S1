using PurrNet;
using UnityEngine;

[RequireComponent(typeof(EnemyBrain))]
[RequireComponent(typeof(EnemyPawn))]
public class StalkerStunnable : NetworkBehaviour, IStunnable
{
    private EnemyBrain brain;
    private EnemyPawn pawn;

    private void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        pawn = GetComponent<EnemyPawn>();
    }

    public void Stun(float durationSeconds)
    {
        if (!isServer) return;
        // Store stun duration for the stunned state
        pawn.SetStunDuration(durationSeconds);
        // Switch enemy into stunned state
        brain.ChangeState(EnemyBrain.StateID.Stunned);
    }
}