using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class ElevatorExit : LevelExitPoint
{
    [Serializable] struct InidicationMessage
    {
        public Color indicationColor;
        public Sprite indicationSprite;
    }

    [SerializeField, Header("Animation Events")] UnityEvent OnFullyClosed;
    [SerializeField] UnityEvent OnFullyOpened, OnStartOpen;
    [SerializeField] Renderer[] anomalyColorIndicators;
    [SerializeField] Image anomalyImage;
    [SerializeField] InidicationMessage anomalyMessage, safeMessage;

    [SerializeField] bool openOnStart;
    AudioSource audioSource;
    Animator anim;

    [Header("Audio")]
    [SerializeField] AudioClip openClip;
    [SerializeField] AudioClip closeClip;

    protected override void Awake()
    {
        base.Awake();
        bHasAnomaly.onChanged += UpdateAnomalyIndicators;
    }
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (asServer) return;

        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        UpdateAnomalyIndicators(bHasAnomaly);

        if (openOnStart) OpenDoors();
    }

    private void UpdateAnomalyIndicators(bool hasAnomaly)
    {
        InidicationMessage indication = hasAnomaly? anomalyMessage : safeMessage;
        foreach (Renderer indicator in anomalyColorIndicators)
        {
            indicator.material.color = indication.indicationColor;
        }

        if (anomalyImage)
        {
            anomalyImage.sprite = indication.indicationSprite;
            anomalyImage.color = indication.indicationColor;
        }
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