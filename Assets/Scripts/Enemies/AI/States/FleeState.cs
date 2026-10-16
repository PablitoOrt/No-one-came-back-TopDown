using System;
using UnityEngine;

public class FleeState : EnemyState
{
    [Serializable]
    public class Settings
    {
        public float speed = 5f;
        [Tooltip("Length of each escape leg.")]
        public float legDistance = 8f;
        [Tooltip("Distance from the threat at which the enemy feels safe.")]
        public float safeDistance = 12f;
        [Tooltip("Gives up fleeing after this many seconds (cornered, no escape route).")]
        public float maxDuration = 8f;
    }

    private readonly Settings settings;
    private float elapsed;

    public FleeState(EnemyBrain brain, Settings settings) : base(brain)
    {
        this.settings = settings;
    }

    private float ThreatDistance => EnemyMotor.FlatDistance(Brain.transform.position, ThreatPosition);

    private Vector3 ThreatPosition => Senses.CanSeeTarget && Senses.Target != null
        ? Senses.Target.transform.position
        : Senses.LastKnownPosition;

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        PickEscapePoint();
    }

    public override void Tick(float deltaTime)
    {
        elapsed += deltaTime;

        bool safe = ThreatDistance >= settings.safeDistance && !Senses.HasTarget;
        if (safe || elapsed >= settings.maxDuration)
        {
            IsComplete = true;
            return;
        }

        if (Motor.HasArrived) PickEscapePoint();
    }

    public override void Exit()
    {
        Motor.Stop();
    }

    private void PickEscapePoint()
    {
        if (Motor.TryGetPointAwayFrom(ThreatPosition, settings.legDistance, out Vector3 point))
        {
            Motor.MoveTo(point, settings.speed);
        }
    }
}
