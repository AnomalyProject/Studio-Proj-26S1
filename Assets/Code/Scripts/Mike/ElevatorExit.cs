using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class ElevatorExit : LevelExitPoint
{
    [SerializeField, Header("Animation Events")] UnityEvent OnFullyClosed;
    [SerializeField] UnityEvent OnFullyOpened, OnStartOpen;

    [SerializeField] bool openOnStart;
    AudioSource audioSource;
    Animator anim;

    [Header("Audio")]
    [SerializeField] AudioClip openClip;
    [SerializeField] AudioClip closeClip;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (openOnStart) OpenDoors();
    }

    [ContextMenu("Open Doors")]
    public void OpenDoors()
    {
        anim.SetTrigger("Open");
        audioSource.Stop();

        if(openClip)
        audioSource.PlayOneShot(openClip);
    }

    [ContextMenu("Close Doors")]
    public void CloseDoors()
    {
        anim.SetTrigger("Close");
        audioSource.Stop();

        if(closeClip)
        audioSource.PlayOneShot(closeClip);
    }
    public void AnimEvent_FullyOpened() => OnFullyOpened?.Invoke();
    public void AnimEvent_FullyClosed() => OnFullyClosed?.Invoke();
    public void AnimEvent_OnStartOpen() => OnStartOpen?.Invoke();
}