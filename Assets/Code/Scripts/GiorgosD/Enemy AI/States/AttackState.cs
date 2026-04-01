using UnityEngine;

public class AttackState : BaseState
{
    private Transform player;

    public AttackState(EnemyBrain brain, EnemyPawn body, Transform player) : base(brain, body)
    {
        this.player = player;
    }

    public override void Enter()
    {
        DoAttack();
    }

    public override void Update()
    {
        
    }

    /// <summary>
    /// Picks random rspawn and does attack andsends you to it.
    /// </summary>
    private void DoAttack()
    {
        int randomIndex = Random.Range(0, brain.RespawnPoints.Count);
        Transform targetPoint = brain.RespawnPoints[randomIndex];

        body.Attack(player, targetPoint);

        brain.ChangeState(new IdleState(brain, body));
    }

    public override void Exit()
    {
        
    }
}
