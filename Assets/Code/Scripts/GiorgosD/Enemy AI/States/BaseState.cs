public class BaseState
{
    protected EnemyBrain brain;
    protected EnemyPawn body;
    protected EnemySounds sound;

    public BaseState(EnemyBrain brain, EnemyPawn body, EnemySounds sound)
    {
        this.brain = brain;
        this.body = body;
        this.sound = sound;
    }

    public virtual void Enter() { body.StopAll(); }
    public virtual void Update() { }
    public virtual void Exit() { }
}
