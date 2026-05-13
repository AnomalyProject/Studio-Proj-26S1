public class BaseState
{
    protected EnemyBrain brain;
    protected EnemyPawn body;

    public BaseState(EnemyBrain brain, EnemyPawn body)
    {
        this.brain = brain;
        this.body = body;
    }

    public virtual void Enter() { body.StopAll(); }
    public virtual void Update() { }
    public virtual void Exit() { }
}
