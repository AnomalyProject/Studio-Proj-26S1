using PurrNet;
using System;
using UnityEngine;

public class AttackState : BaseState
{
    private Transform target;

    public AttackState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log("Attacking");
        
        target = brain.TargetPos;
        body.OnStartAttack.AddListener(DoAttack);

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
        
        if (body.IsHitSuccess(target))
        {
            int randomIndex = UnityEngine.Random.Range(0, brain.RespawnPoints.Count);
            Transform targetPoint = brain.RespawnPoints[randomIndex];

            var playerID = target.GetComponent<NetworkIdentity>();

            body.TeleportToSpawn(targetPoint.position, playerID);

            Debug.Log("Player Attacked");
        
            body.InvokeAttacked(target.GetComponent<PlayerBody>());

            brain.ChangeState(EnemyBrain.StateID.Idle);
        }
        else
        {
            ChangeToChaseState();
        }
    }

    private void ChangeToChaseState()
    {
        brain.ChangeState(EnemyBrain.StateID.Chase, target);
    }

    public override void Exit()
    {
        body.OnStartAttack.RemoveListener(DoAttack);
    }
}
