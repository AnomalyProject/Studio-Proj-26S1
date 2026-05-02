using UnityEngine;

public class InvestigateState : BaseState
{
    private Vector3 lastKnownPos;
    private Transform player;

    public InvestigateState(EnemyBrain brain, EnemyPawn body, Vector3 lastPos, Transform player) : base(brain, body)
    {
        this.lastKnownPos = lastPos;
        this.player = player;
    }

    public override void Enter()
    {
        body.SetMoveSpeed(true);

        body.MoveToTarget(lastKnownPos);

        body.OnPlayerSpotted += PlayerFound;
    }
    public override void Update()
    {
        // If the player is close enough, switch to chase state because sometimes when the it reaches the player it goes idle.
        if (Vector3.Distance(body.transform.position, player.position) < 3.0f && player.CompareTag("Player"))
        {
            brain.ChangeState(new ChaseState(brain, body, player));
            return;
        }

        if (body.agent.remainingDistance <= body.agent.stoppingDistance)
        {
           brain.ChangeState(new IdleState(brain, body));
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
