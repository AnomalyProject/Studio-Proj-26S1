using System.Collections.Generic;
using UnityEngine;

public class PatrolState : BaseState
{
    private static int pointIndex = -1;

    public PatrolState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {

    }

    public override void Enter()
    {
        base.Enter();

        body.SetMoveSpeed(false);
        body.anim.SetBool("IsWalk", true);

        body.OnPlayerSpotted.AddListener(HandlePlayerSpotted);

        if (brain.poiIsEnabled)
        {
            MoveToPoint(brain.PatrolPriorities);
        }
        else
        {
            MoveToPoint(brain.PatrolPoints);
        }
    }

    /// <summary>
    /// Transition to Idle if close enough to the patrol point.
    /// </summary>
    public override void Update()
    {
        if (body.agent.hasPath && body.agent.remainingDistance <= body.agent.stoppingDistance)
        {
            brain.ChangeState(EnemyBrain.StateID.Idle);
            return;
        }
    }

    /// <summary>
    /// Selects a new patrol point at random and moves to it.
    /// </summary>
    /// <param name="patrolPoints"> The list of patrol points to choose from. </param>
    private void MoveToPoint(List<Transform> patrolPoints)
    {
        if (patrolPoints.Count == 0) return;

        int nextIndex = pointIndex;

        while (nextIndex == pointIndex && patrolPoints.Count > 1)
        {
            nextIndex = Random.Range(0, patrolPoints.Count);
        }

        pointIndex = nextIndex;

        Debug.Log($"Moving to patrol point {pointIndex}");

        body.MoveToTarget(patrolPoints[pointIndex].position);
    }

    private void HandlePlayerSpotted(PlayerBody player)
    {
        brain.ChangeState(EnemyBrain.StateID.Alert, player.transform);
    }

    public override void Exit()
    {
        body.anim.SetBool("IsWalk", false);
        body.OnPlayerSpotted.RemoveListener(HandlePlayerSpotted);
    }
}