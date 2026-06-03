using UnityEngine;
using UnityEngine.UIElements;

public class ChaseState : BaseState
{
    private Transform target;

    public ChaseState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        body.SetMoveSpeed(true);
        body.anim.SetBool("IsRun", true);
        
        body.OnLostPlayer.AddListener(LostPlayer);
    }

    /// <summary>
    /// Updates the players pos can find a way to not use ifs on this one.
    /// </summary>
    public override void Update()
    {
        target = brain.TargetPos;

        if (target == null) 
        {
            brain.ChangeState(EnemyBrain.StateID.Idle);
            return;
        }

        body.RotateTowards(target.position);
        body.MoveToTarget(target.position);

        if (Vector3.Distance(body.transform.position, target.position) <= body.agent.stoppingDistance)
        {
            brain.ChangeState(EnemyBrain.StateID.Attack, target);
        }
    }

    /// <summary>
    /// gives the last known position of the player and starts searching for him.
    /// </summary>
    private void LostPlayer()
    {
        brain.ChangeState(EnemyBrain.StateID.Investigate, target);
    }

    public override void Exit()
    {
        body.anim.SetBool("IsRun", false);
        body.OnLostPlayer.RemoveListener(LostPlayer);
    }
}
