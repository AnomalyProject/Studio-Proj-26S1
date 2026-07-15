using UnityEngine;

public class InvestigateState : BaseState
{
    private Vector3 lastKnownPos;
    private Transform target;

    public InvestigateState(EnemyBrain brain, EnemyPawn body, EnemySounds sound) : base(brain, body, sound)
    {

    }

    public override void Enter()
    {
        base.Enter();

        body.SetMoveSpeed(true);
        body.anim.SetBool("IsRun", true);

        lastKnownPos = brain.TargetPos.position;
        
        body.MoveToTarget(lastKnownPos, true);

        body.OnPlayerSpotted.AddListener(PlayerFound);
    }

    public override void Update()
    {
        if (body.agent.pathPending) return;

        target = brain.TargetPos;

        // If the player is close enough, switch to chase state because sometimes when it reaches the player it goes idle.
        if (Vector3.Distance(body.transform.position, target.position) < 3.0f && target.CompareTag("Player"))
        {
            brain.ChangeState(EnemyBrain.StateID.Chase, target);
            return;
        }

        if (body.agent.remainingDistance <= body.agent.stoppingDistance && body.agent.hasPath)
        {
            brain.ChangeState(EnemyBrain.StateID.Idle);
        }
        else if (!body.agent.hasPath)
        {
            brain.ChangeState(EnemyBrain.StateID.Idle);
        }
    }

    private void PlayerFound(PlayerBody player)
    {
        brain.ChangeState(EnemyBrain.StateID.Chase, player.transform);
    }

    public override void Exit()
    {
        body.anim.SetBool("IsRun", false);
        body.OnPlayerSpotted.RemoveListener(PlayerFound);
    }
}
