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

        bool isHit = body.IsHitSuccess(target) ? DoAttack(target) : ChangeToChaseState();
        
        body.anim.SetBool("isAttack", true);
    }

    public override void Update()
    {

    }

    /// <summary>
    /// Picks random respawn and does attack and sends you to it.
    /// </summary>
    private bool DoAttack(Transform player)
    {
        int randomIndex = UnityEngine.Random.Range(0, brain.RespawnPoints.Count);
        Transform targetPoint = brain.RespawnPoints[randomIndex];

        var playerID = player.GetComponent<NetworkIdentity>();

        body.TeleportToSpawn(targetPoint.position, playerID);

        Debug.Log("Player Attacked");
        
        body.InvokeAttacked(player.GetComponent<PlayerBody>());

        brain.ChangeState(EnemyBrain.StateID.Idle);

        return true;
    }

    private bool ChangeToChaseState()
    {
        brain.ChangeState(EnemyBrain.StateID.Chase, target);

        return true;
    }

    public override void Exit()
    {
        body.anim.SetBool("isAttack", false);
    }
}
