using UnityEngine;

public class DistractedState : BaseState
{
    public DistractedState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {
    }

    public override void Enter()
    {
        base.Enter();

        body.SetMoveSpeed(true);
        
        body.anim.SetBool("isRun", true);
        
        body.MoveToTarget(brain.TargetPos.position);
    }

    public override void Update()
    {
        if (body.agent.remainingDistance > body.agent.stoppingDistance) return;
        
        // Hack
        body.anim.SetBool("isRun", false);
        // Hack
        
        if (brain.TargetPos == null)
        {
            brain.ChangeState(EnemyBrain.StateID.Patrol);
        }
    }

    public override void Exit()
    {
        
    }
}
