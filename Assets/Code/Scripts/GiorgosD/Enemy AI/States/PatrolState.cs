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

        MoveToPoint();
    }

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
    private void MoveToPoint()
    {
        if (brain.PatrolPoints.Count == 0) return;

        int nextIndex = pointIndex;

        while (nextIndex == pointIndex && brain.PatrolPoints.Count > 1)
        {
            nextIndex = Random.Range(0, brain.PatrolPoints.Count);
        }

        pointIndex = nextIndex;

        Debug.Log($"Moving to patrol point {pointIndex}");

        body.MoveToTarget(brain.PatrolPoints[pointIndex].position);
    }

    /// <summary>
    /// Transition to Idle and then from idle to patrol again reseting the random point.
    /// </summary>
    private void MoveToNextPoint()
    {
        brain.ChangeState(new IdleState(brain, body));
    }

    private void HandlePlayerSpotted(GameObject player)
    {
        brain.ChangeState(new ChaseState(brain, body, player.transform));
    }

    public override void Exit()
    {
        body.OnPlayerSpotted -= HandlePlayerSpotted;
    }
}