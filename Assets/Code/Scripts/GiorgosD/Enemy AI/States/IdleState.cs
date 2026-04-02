using UnityEngine;

public class IdleState : BaseState
{
    public IdleState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {

    }

    public override void Enter()
    {
        body.OnPlayerSpotted += HandlePlayerSpotted;
        body.OnIdleEnd += ResumePatrol;

        body.Idle(brain.IdleTime);
    }

    public override void Update()
    {
        
    }

    private void ResumePatrol() => brain.ChangeState(new PatrolState(brain, body));
    

    private void HandlePlayerSpotted(Transform player) => brain.ChangeState(new ChaseState(brain, body, player));
    

    public override void Exit()
    {
        body.OnPlayerSpotted -= HandlePlayerSpotted;
        body.OnIdleEnd -= ResumePatrol;
    }
}