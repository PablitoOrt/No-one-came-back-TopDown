public class AttackState : EnemyState
{
    private readonly IEnemyAttack attack;

    public AttackState(EnemyBrain brain, IEnemyAttack attack) : base(brain)
    {
        this.attack = attack;
    }

    public override void Enter()
    {
        base.Enter();
        Motor.Stop();
    }

    public override void Tick(float deltaTime)
    {
        Health target = Senses.Target;
        if (target == null) return;

        Motor.FaceTowards(target.transform.position, deltaTime);

        if (attack.CanStartAttack(target)) attack.StartAttack(target);
    }

    public override void Exit()
    {
        attack.Cancel();
    }
}
