using UnityEngine;

public class AlertState : BaseState
{
    private Transform target;
    public AlertState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {

    }

    public override void Enter()
    {
        target = brain.TargetPos;
        
        // Play sound and/or animation to indicate the enemy is alert.

        body.OnPlayerSpotted += PlayerFound;
    }

    public override void Update()
    {
        body.RotateTowards(target.position);

        if (body.IsFacingTarget(target.position))
        {
            brain.ChangeState(EnemyBrain.StateID.Investigate, target);
        }
    }

    private void PlayerFound(GameObject player)
    {
       brain.ChangeState(EnemyBrain.StateID.Chase, player.transform);
    }

    public override void Exit()
    {
        body.OnPlayerSpotted -= PlayerFound;
    }
}
