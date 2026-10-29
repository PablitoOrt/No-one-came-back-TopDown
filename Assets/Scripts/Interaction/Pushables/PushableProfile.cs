using UnityEngine;

[CreateAssetMenu(menuName = "Interaction/Pushable Profile", fileName = "NewPushableProfile")]
public class PushableProfile : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Player walk speed multiplier while pushing. Heavier objects = lower value.")]
    [SerializeField, Range(0.05f, 1f)] private float speedMultiplier = 0.5f;
    [SerializeField] private bool allowSprint = false;
    [Tooltip("Speed (m/s) above which the object counts as moving, for audio and events.")]
    [SerializeField, Min(0f)] private float movingSpeedThreshold = 0.05f;

    [Header("Grab")]
    [Tooltip("Snap the grab direction to the object's local faces (good for boxes). Off = grab from any angle.")]
    [SerializeField] private bool snapGrabToFaces = true;
    [Tooltip("The player turns to face the object while grabbing it.")]
    [SerializeField] private bool facePusher = true;
    [Tooltip("Auto-release when the player ends up this much farther from the object than when grabbing it.")]
    [SerializeField, Min(0f)] private float releaseDistanceTolerance = 0.5f;

    [Header("Player")]
    [Tooltip("The player can't aim, shoot or swing while grabbing.")]
    [SerializeField] private bool blocksCombat = true;

    public float SpeedMultiplier => speedMultiplier;
    public bool AllowSprint => allowSprint;
    public float MovingSpeedThreshold => movingSpeedThreshold;
    public bool SnapGrabToFaces => snapGrabToFaces;
    public bool FacePusher => facePusher;
    public float ReleaseDistanceTolerance => releaseDistanceTolerance;
    public bool BlocksCombat => blocksCombat;
}
