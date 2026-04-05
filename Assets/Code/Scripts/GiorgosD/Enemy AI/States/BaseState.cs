public abstract class BaseState
{
    protected EnemyBrain brain;
    protected EnemyPawn body;

    public BaseState(EnemyBrain brain, EnemyPawn body)
    {
        this.brain = brain;
        this.body = body;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
