using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using PurrNet;
using System;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class ElevatorExit : LevelExitPoint
{
    [Serializable] struct InidicationMessage
    {
        public Color indicationColor;
        public Sprite indicationSprite;
    }

    [SerializeField, Header("Animation Events")] UnityEvent OnFullyClosed;
    [SerializeField] private UnityEvent OnFullyOpened, OnStartOpen;
    [SerializeField] private Renderer[] anomalyColorIndicators;
    [SerializeField] private Image anomalyImage;
    [SerializeField] private InidicationMessage anomalyMessage, safeMessage;

    [SerializeField] private Renderer[] avatars;
    [SerializeField] private Material avatarGreenMat, avatarRedMat;

    [SerializeField] private bool openOnStart;
    private AudioSource audioSource;
    private Animator anim;

    [Header("Audio")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    protected override void Awake()
    {
        base.Awake();
        bHasAnomaly.onChanged += UpdateAnomalyIndicators;

        foreach (Renderer avatar in avatars) avatar.gameObject.SetActive(false);
        for (int i = 0; i < NetworkManager.main.playerCount; i++) avatars[i].gameObject.SetActive(true);
        OnPlayersChanged.AddListener(UpdateAvatarColors);

        SessionEvents.OnPlayerJoined += UpdateAvatarCount;
        SessionEvents.OnPlayerLeft += UpdateAvatarCount;
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
    private void UpdateAvatarColors(bool isReady)
    {
        for (int i = 0; i < NetworkManager.main.playerCount; i++) avatars[i].material = i < playersInArea.Count ? avatarGreenMat : avatarRedMat;
    }
    private void UpdateAvatarCount(ulong steamID, string displayName)
    {
        foreach (Renderer avatar in avatars) avatar.gameObject.SetActive(false);
        for (int i = 0; i < NetworkManager.main.playerCount; i++) avatars[i].gameObject.SetActive(true);
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