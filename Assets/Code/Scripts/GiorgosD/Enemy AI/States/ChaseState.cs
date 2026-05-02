using UnityEngine;
using UnityEngine.UIElements;

public class ChaseState : BaseState
{
    private Transform player;

    public ChaseState(EnemyBrain brain, EnemyPawn body, Transform player) : base(brain, body)
    {
        this.player = player;
    }

    public override void Enter()
    {
        body.SetMoveSpeed(true);

        body.OnLostPlayer += LostPlayer;
    }

    /// <summary>
    /// Updates the players pos can find a way to not use ifs on this one.
    /// </summary>
    public override void Update()
    {
        if (player == null) return;

        body.RotateTowards(player.position);
        body.MoveToTarget(player.position);

        if (Vector3.Distance(body.transform.position, player.position) <= 2.0f)
        {
            brain.ChangeState(new AttackState(brain, body, player));
        }
    }

    /// <summary>
    /// gives the last known position of the player and starts searching for him.
    /// </summary>
    private void LostPlayer()
    {
        brain.ChangeState(new InvestigateState(brain, body, player.position, player));
    }

    public override void Exit()
    {
        body.OnLostPlayer -= LostPlayer;
    }
}
