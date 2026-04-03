using System;
using UnityEngine;

public class AttackState : BaseState
{
    private Transform player;
    public event Action<Transform> OnPlayerAttacked;

    public AttackState(EnemyBrain brain, EnemyPawn body, Transform player) : base(brain, body)
    {
        this.player = player;
    }

    public override void Enter()
    {
        //OnPlayerAttacked?.Invoke(player); //this if we want the attack event to happen even if not hit.

                    
        bool isHit = body.IsHitSuccess(player) ? DoAttack(player) : ChangeToChaseState();
    }

    public override void Update()
    {
        
    }

    /// <summary>
    /// Picks random rspawn and does attack andsends you to it.
    /// </summary>
    private bool DoAttack(Transform player)
    {
        int randomIndex = UnityEngine.Random.Range(0, brain.RespawnPoints.Count);
        Transform targetPoint = brain.RespawnPoints[randomIndex];

        var controller = player.GetComponent<CharacterController>();

        if(controller != null)
        {
            controller.enabled = false;
        }

        player.position = targetPoint.position;

        if(controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log("Player Attacked");

        OnPlayerAttacked?.Invoke(player);   //this if we want event to happen only if hit is successful.

        brain.ChangeState(new IdleState(brain, body));

        return true;
    }

    private bool ChangeToChaseState()
    {
        brain.ChangeState(new ChaseState(brain, body, player));

        return true;
    }

    public override void Exit()
    {
        
    }
}
