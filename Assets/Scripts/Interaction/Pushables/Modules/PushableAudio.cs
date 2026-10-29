using UnityEngine;

[RequireComponent(typeof(PushableObject))]
public class PushableAudio : MonoBehaviour
{
    [Tooltip("Left empty, auto-resolved from this object.")]
    [SerializeField] private AudioSource source;
    [Tooltip("Looping scrape played while the object moves.")]
    [SerializeField] private AudioClip movingLoop;
    [SerializeField] private AudioClip grabClip;
    [SerializeField] private AudioClip releaseClip;

    private PushableObject pushable;

    private void Awake()
    {
        pushable = GetComponent<PushableObject>();
        if (source == null) source = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        pushable.Grabbed += HandleGrabbed;
        pushable.Released += HandleReleased;
        pushable.StartedMoving += HandleStartedMoving;
        pushable.StoppedMoving += HandleStoppedMoving;
    }

    private void OnDisable()
    {
        pushable.Grabbed -= HandleGrabbed;
        pushable.Released -= HandleReleased;
        pushable.StartedMoving -= HandleStartedMoving;
        pushable.StoppedMoving -= HandleStoppedMoving;
    }

    private void HandleGrabbed(PushableObject _) => PlayOneShot(grabClip);

    private void HandleReleased(PushableObject _) => PlayOneShot(releaseClip);

    private void HandleStartedMoving(PushableObject _)
    {
        if (source == null || movingLoop == null) return;

        source.clip = movingLoop;
        source.loop = true;
        source.Play();
    }

    private void HandleStoppedMoving(PushableObject _)
    {
        if (source != null && source.clip == movingLoop) source.Stop();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }
}
