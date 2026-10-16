using UnityEngine;

[RequireComponent(typeof(EnemyMeleeAttack))]
public class BasicEnemyBrain : EnemyBrain
{
    [Header("States")]
    [SerializeField] private PatrolState.Settings patrol = new PatrolState.Settings();
    [SerializeField] private IdleState.Settings rest = new IdleState.Settings();
    [Tooltip("Pause after losing the target before going back to patrol.")]
    [SerializeField] private IdleState.Settings search = new IdleState.Settings { durationRange = new Vector2(1.5f, 3f) };
    [SerializeField] private ChaseState.Settings chase = new ChaseState.Settings();
    [SerializeField] private FleeState.Settings flee = new FleeState.Settings();

    [Header("Decisions")]
    [SerializeField] private LowHealthFleeRule fleeRule = new LowHealthFleeRule();

    private bool wantsToFlee;

    protected override IState BuildStateMachine(StateMachine machine)
    {
        IEnemyAttack attack = GetComponent<IEnemyAttack>();

        var patrolState = new PatrolState(this, patrol);
        var restState = new IdleState(this, rest);
        var searchState = new IdleState(this, search, "Search");
        var chaseState = new ChaseState(this, chase);
        var attackState = new AttackState(this, attack);
        var fleeState = new FleeState(this, flee);

        bool HasTarget() => Senses.HasTarget;
        bool InAttackRange() => attack.IsInRange(Senses.Target);

        machine.AddAnyTransition(fleeState, () => wantsToFlee);

        machine.AddTransition(patrolState, chaseState, HasTarget);
        machine.AddTransition(patrolState, restState, () => patrolState.IsComplete);

        machine.AddTransition(restState, chaseState, HasTarget);
        machine.AddTransition(restState, patrolState, () => restState.IsComplete);

        machine.AddTransition(chaseState, attackState, () => HasTarget() && InAttackRange());
        machine.AddTransition(chaseState, searchState, () => !HasTarget());

        machine.AddTransition(attackState, chaseState, () => !attack.IsBusy && HasTarget() && !InAttackRange());
        machine.AddTransition(attackState, searchState, () => !attack.IsBusy && !HasTarget());

        machine.AddTransition(searchState, chaseState, HasTarget);
        machine.AddTransition(searchState, patrolState, () => searchState.IsComplete);

        machine.AddTransition(fleeState, searchState, () => fleeState.IsComplete);

        machine.StateChanged += (previous, _) =>
        {
            if (previous == fleeState) wantsToFlee = false;
        };

        return patrolState;
    }

    protected override void OnDamaged(DamageInfo damage)
    {
        if (!wantsToFlee && fleeRule.RollOnHit(Health)) wantsToFlee = true;
    }
}
