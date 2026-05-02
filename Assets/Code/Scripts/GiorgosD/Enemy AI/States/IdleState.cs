using UnityEngine;

public class IdleState : BaseState
{
    private float timer;
    public IdleState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {
        
    }

    public override void Enter()
    {
        body.IdleSearch(true);
        // Idle animation/search animation (looking left and right animation)
        timer = brain.IdleTime;
        body.OnPlayerSpotted += HandlePlayerSpotted;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            brain.ChangeState(new PatrolState(brain, body));
        }
    }
    
    private void HandlePlayerSpotted(GameObject player) => brain.ChangeState(new AlertState(brain, body, player.transform));
    

    public override void Exit()
    {
        body.OnPlayerSpotted -= HandlePlayerSpotted;

        body.IdleSearch(false);
    }
}