public abstract class EnemyState : IState
{
    protected readonly EnemyBrain Brain;

    protected EnemyState(EnemyBrain brain)
    {
        Brain = brain;
    }

    public bool IsComplete { get; protected set; }
    public virtual string Name => GetType().Name.Replace("State", "");

    protected EnemyMotor Motor => Brain.Motor;
    protected EnemySenses Senses => Brain.Senses;

    public virtual void Enter()
    {
        IsComplete = false;
    }

    public virtual void Tick(float deltaTime) { }

    public virtual void Exit() { }
}
