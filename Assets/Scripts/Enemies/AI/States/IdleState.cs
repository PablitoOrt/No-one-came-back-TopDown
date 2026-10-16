using System;
using UnityEngine;

public class IdleState : EnemyState
{
    [Serializable]
    public class Settings
    {
        public Vector2 durationRange = new Vector2(1.5f, 4f);
    }

    private readonly Settings settings;
    private readonly string name;
    private float remaining;

    public IdleState(EnemyBrain brain, Settings settings, string name = "Idle") : base(brain)
    {
        this.settings = settings;
        this.name = name;
    }

    public override string Name => name;

    public override void Enter()
    {
        base.Enter();
        Motor.Stop();
        remaining = UnityEngine.Random.Range(settings.durationRange.x, settings.durationRange.y);
    }

    public override void Tick(float deltaTime)
    {
        remaining -= deltaTime;
        if (remaining <= 0f) IsComplete = true;
    }
}
