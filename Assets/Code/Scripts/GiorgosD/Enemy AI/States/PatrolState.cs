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
        body.SetMoveSpeed(false);

        body.OnPlayerSpotted += HandlePlayerSpotted;

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
        if (!body.agent.pathPending && body.agent.remainingDistance <= body.agent.stoppingDistance)
        {
            brain.ChangeState(new IdleState(brain, body));
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

    private void HandlePlayerSpotted(GameObject player)
    {
        brain.ChangeState(new AlertState(brain, body, player.transform));
    }

    public override void Exit()
    {
        body.OnPlayerSpotted -= HandlePlayerSpotted;
    }
}