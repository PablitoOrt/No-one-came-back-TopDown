using UnityEngine;

public readonly struct PushContext
{
    public PushContext(PushableObject pushable, PlayerPusher pusher, Vector3 grabNormal)
    {
        Pushable = pushable;
        Pusher = pusher;
        GrabNormal = grabNormal;
    }

    public PushableObject Pushable { get; }
    public PlayerPusher Pusher { get; }
    public Vector3 GrabNormal { get; }
    public Vector3 PushAxis => -GrabNormal;
}
