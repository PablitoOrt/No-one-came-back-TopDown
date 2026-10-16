using System;
using UnityEngine;

public class ChaseState : EnemyState
{
    [Serializable]
    public class Settings
    {
        public float speed = 4.5f;
        [Tooltip("Seconds between path updates - lower is more responsive, higher is cheaper.")]
        public float repathInterval = 0.2f;
    }

    private readonly Settings settings;
    private float repathTimer;

    public ChaseState(EnemyBrain brain, Settings settings) : base(brain)
    {
        this.settings = settings;
    }

    public override void Enter()
    {
        base.Enter();
        repathTimer = 0f;
    }

    public override void Tick(float deltaTime)
    {
        repathTimer -= deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = settings.repathInterval;

        Vector3 destination = Senses.CanSeeTarget && Senses.Target != null
            ? Senses.Target.transform.position
            : Senses.LastKnownPosition;

        Motor.MoveTo(destination, settings.speed);
    }

    public override void Exit()
    {
        Motor.Stop();
    }
}
