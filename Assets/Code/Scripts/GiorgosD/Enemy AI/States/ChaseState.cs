using UnityEngine;

public class ChaseState : BaseState
{
    private Transform player;
    private Vector3 lastKnownPos;
    private bool isSearching;
    public ChaseState(EnemyBrain brain, EnemyPawn body, Transform player) : base(brain, body)
    {
        this.player = player;
    }

    public override void Enter()
    {
        body.SetMoveSpeed(true);

        body.OnTargetReached += CatchPlayer;
        body.OnLostPlayer += LostPlayer;

        lastKnownPos = player.position;
        body.MoveToTarget(player.position);
    }

    /// <summary>
    /// Updates the players pos can find a way to not use ifs on this one.
    /// </summary>
    public override void Update()
    {
        if (player == null) return;

        if (!isSearching)
        {
            if (Vector3.Distance(lastKnownPos, player.position) > 1.0f)
            {
                lastKnownPos = player.position;
                body.MoveToTarget(player.position);
            }
        }
    }

    /// <summary>
    /// Transitions between states depending on if the player was caught or flead.
    /// </summary>
    private void CatchPlayer()
    {
        if (!isSearching)
        {
            brain.ChangeState(new AttackState(brain, body, player));
        }
        else
        {
            brain.ChangeState(new IdleState(brain, body));
        }
    }

    /// <summary>
    /// gives the last known position of the player and starts searching for him.
    /// </summary>
    private void LostPlayer()
    {
        if (isSearching) return;

        isSearching = true;

        lastKnownPos = player.position;

        body.MoveToTarget(lastKnownPos);
    }

    public override void Exit()
    {
        body.OnTargetReached -= CatchPlayer;
        body.OnLostPlayer -= LostPlayer;
    }
}
