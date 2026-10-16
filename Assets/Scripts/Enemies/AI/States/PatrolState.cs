using System;
using UnityEngine;

public class PatrolState : EnemyState
{
    [Serializable]
    public class Settings
    {
        public float speed = 2f;
        [Tooltip("Left empty, wanders to random points within Wander Radius of the spawn.")]
        public Transform[] waypoints;
        public bool randomWaypointOrder;
        public float wanderRadius = 8f;
        [Tooltip("Chance (0-1) to stop and idle each time a patrol point is reached.")]
        [Range(0f, 1f)] public float restChance = 0.35f;
        [Tooltip("Picks a new point if one isn't reached within this many seconds (blocked path).")]
        public float stuckTimeout = 8f;
    }

    private readonly Settings settings;
    private int waypointIndex = -1;
    private float legTime;

    public PatrolState(EnemyBrain brain, Settings settings) : base(brain)
    {
        this.settings = settings;
    }

    private bool HasWaypoints => settings.waypoints != null && settings.waypoints.Length > 0;

    public override void Enter()
    {
        base.Enter();
        PickNextPoint();
    }

    public override void Tick(float deltaTime)
    {
        legTime += deltaTime;

        if (Motor.HasArrived)
        {
            if (UnityEngine.Random.value < settings.restChance) IsComplete = true;
            else PickNextPoint();
        }
        else if (legTime > settings.stuckTimeout)
        {
            PickNextPoint();
        }
    }

    private void PickNextPoint()
    {
        legTime = 0f;

        if (HasWaypoints)
        {
            waypointIndex = settings.randomWaypointOrder
                ? UnityEngine.Random.Range(0, settings.waypoints.Length)
                : (waypointIndex + 1) % settings.waypoints.Length;

            Transform waypoint = settings.waypoints[waypointIndex];
            if (waypoint != null) Motor.MoveTo(waypoint.position, settings.speed);
            return;
        }

        if (Motor.TryGetRandomPoint(Brain.HomePosition, settings.wanderRadius, out Vector3 point))
        {
            Motor.MoveTo(point, settings.speed);
        }
    }
}
