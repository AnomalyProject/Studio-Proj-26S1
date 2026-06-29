using UnityEngine;

public class AttackState : BaseState
{
    private Transform target;
    private bool hasHitTarget;

    public AttackState(EnemyBrain brain, EnemyPawn body, EnemySounds sound) : base(brain, body, sound)
    {
    }

    public override void Enter()
    {
        base.Enter();

        target = brain.TargetPos;
        
        if (body.IsAttackCooldown)
        {
            brain.ChangeState(EnemyBrain.StateID.Chase, target);
            return;
        }
        
        sound.SelectGrowl(brain.CurrentStateID);
        
        hasHitTarget = false;
        
        body.OnStartAttack.AddListener(DoAttack);
        body.OnEndAttack.AddListener(Outcome);

        body.anim.SetTrigger("Attack");
    }

    public override void Update()
    {
        if (target == null) return;
        
        body.RotateTowards(target.position);
    }
    
    /// <summary>
    /// Picks random respawn and does attack and sends you to it.
    /// </summary>
    private void DoAttack()
    {
        if (target == null) return;
        
        PlayerBody player = target.GetComponent<PlayerBody>();
        if (player != null && player.Invis.IsInvis)
        {
            hasHitTarget = false;
            return; 
        }
        
        if (body.IsHitSuccess(target))
        {
            int randomIndex = Random.Range(0, brain.RespawnPoints.Count);
            Transform targetPoint = brain.RespawnPoints[randomIndex];
            
            hasHitTarget = true;

            body.TeleportToSpawn(targetPoint.position, player);

            Debug.Log("Player Attacked");
        
            body.InvokeAttacked(target.GetComponent<PlayerBody>());
        }
        else
        {
            hasHitTarget = false;
        }
    }

    private void Outcome()
    {
        bool playerInvis = body.CachedPlayer != null && body.CachedPlayer.Invis.IsInvis;

        if (playerInvis || hasHitTarget)
        {
            brain.ChangeState(EnemyBrain.StateID.Idle);
        }
        else
        {
            brain.ChangeState(EnemyBrain.StateID.Chase, target);
        }
    }

    public override void Exit()
    {
        body.OnStartAttack.RemoveListener(DoAttack);
        body.OnEndAttack.RemoveListener(Outcome);
    }
}
