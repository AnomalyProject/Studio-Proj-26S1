
using UnityEngine;

public class StunnedState : BaseState
{
    private float timer;

    public StunnedState(EnemyBrain brain, EnemyPawn body, EnemySounds sound) : base(brain, body, sound)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // Start stun countdown
        timer = body.StunDuration;

        sound.SelectGrowl();

        if (body.anim != null)
            body.anim.SetBool("IsStunned", true);
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        // Return to idle when stun expires
        if (timer <= 0f)
            brain.ChangeState(EnemyBrain.StateID.Idle);
    }

    public override void Exit()
    {
        // Reset stun animation state
        if (body.anim != null)
            body.anim.SetBool("IsStunned", false);
    }
}