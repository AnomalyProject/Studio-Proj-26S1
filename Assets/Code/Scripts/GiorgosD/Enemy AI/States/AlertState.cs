using UnityEngine;

public class AlertState : BaseState
{
    private Transform player;
    public AlertState(EnemyBrain brain, EnemyPawn body, Transform player) : base(brain, body)
    {
        this.player = player;
    }

    public override void Enter()
    {
        // Play sound and/or animation to indicate the enemy is alert.

        body.OnPlayerSpotted += PlayerFound;
    }

    public override void Update()
    {
        body.RotateTowards(player.position);

        if (body.IsFacingTarget(player.position))
        {
            brain.ChangeState(new InvestigateState(brain, body, player.position, player));
        }
    }

    private void PlayerFound(GameObject player)
    {
        brain.ChangeState(new ChaseState(brain, body, player.transform));
    }

    public override void Exit()
    {
        body.OnPlayerSpotted -= PlayerFound;
    }
}
