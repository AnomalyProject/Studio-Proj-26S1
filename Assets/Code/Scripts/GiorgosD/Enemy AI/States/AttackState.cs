using System;
using UnityEngine;

public class AttackState : BaseState
{
    private Transform target;
    public event Action<GameObject> OnPlayerAttacked;

    public AttackState(EnemyBrain brain, EnemyPawn body) : base(brain, body)
    {
        
    }

    public override void Enter()
    {
        Debug.Log("Attacking");
        //OnPlayerAttacked?.Invoke(player.gameObject); //this if we want the attack event to happen even if not hit.

        target = brain.TargetPos;

        bool isHit = body.IsHitSuccess(target) ? DoAttack(target) : ChangeToChaseState();
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

        var controller = player.gameObject.GetComponent<CharacterController>();

        if (controller != null) 
        {
            controller.enabled = false;
        }

        player.position = targetPoint.position;

        if(player != null)
        {
            controller.enabled = true;
        }

        Debug.Log("Player Attacked");

        OnPlayerAttacked?.Invoke(player.gameObject);   //this if we want event to happen only if hit is successful.

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

    }
}
