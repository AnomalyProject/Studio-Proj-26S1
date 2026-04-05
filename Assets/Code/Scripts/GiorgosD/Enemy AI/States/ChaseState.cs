using UnityEngine;
using UnityEngine.UIElements;

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

        body.OnLostPlayer += LostPlayer;
        body.OnPlayerSpotted += PlayerFound;

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
            body.RotateTowards(player.position);

            if (Vector3.Distance(lastKnownPos, player.position) > 1.0f)
            {
                lastKnownPos = player.position;
                body.MoveToTarget(player.position);
            }

            if (Vector3.Distance(body.transform.position, player.position) <= 2.0f)
            {
                brain.ChangeState(new AttackState(brain, body, player));
            }
        }
        else
        {
            if (Vector3.Distance(body.transform.position, lastKnownPos) <= 2.0f)
            {
                brain.ChangeState(new IdleState(brain, body));
            }
        }
    }

    /// <summary>
    /// gives the last known position of the player and starts searching for him.
    /// </summary>
    private void LostPlayer()
    {
        if (isSearching) return;

        isSearching = true;

        body.MoveToTarget(lastKnownPos);
    }

    /// <summary>
    /// Safty because it ignores you when it turns the corner without it.
    /// </summary>
    /// <param name="player"></param>
    private void PlayerFound(GameObject player)
    {
        isSearching = false;
    }

    public override void Exit()
    {
        body.OnLostPlayer -= LostPlayer;
        body.OnPlayerSpotted -= PlayerFound;
    }
}
