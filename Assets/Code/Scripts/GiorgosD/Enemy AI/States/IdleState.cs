using UnityEngine;

public class IdleState : BaseState
{
    private float timer;
    public IdleState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        body.Search(true);
        // Idle animation/search animation (looking left and right animation)
        timer = brain.IdleTime;
        body.OnPlayerSpotted += HandlePlayerSpotted;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            brain.ChangeState(EnemyBrain.StateID.Patrol);
        }
    }

    private void HandlePlayerSpotted(GameObject player)
    {
        brain.ChangeState(EnemyBrain.StateID.Alert, player.transform);
    }

    public override void Exit()
    {
        body.OnPlayerSpotted -= HandlePlayerSpotted;

        body.Search(false);
    }
}