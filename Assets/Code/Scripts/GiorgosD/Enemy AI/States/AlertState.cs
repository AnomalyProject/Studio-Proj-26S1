using UnityEngine;

public class AlertState : BaseState
{
    private Transform target;
    public AlertState(EnemyBrain brain, EnemyPawn body, EnemySounds sound) : base(brain, body, sound)
    {

    }

    public override void Enter()
    {
        base.Enter();

        target = brain.TargetPos;
        
        sound.SelectGrowl();
        
        body.OnPlayerSpotted.AddListener(PlayerFound);
    }

    public override void Update()
    {
        body.RotateTowards(target.position);

        if (body.IsFacingTarget(target.position))
        {
            brain.ChangeState(EnemyBrain.StateID.Investigate, target);
        }
    }

    private void PlayerFound(PlayerBody player)
    {
        brain.ChangeState(EnemyBrain.StateID.Chase, player.transform);
    }

    public override void Exit()
    {
        body.OnPlayerSpotted.RemoveListener(PlayerFound);
    }
}
