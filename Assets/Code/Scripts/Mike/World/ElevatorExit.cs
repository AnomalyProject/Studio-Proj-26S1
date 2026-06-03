using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;
using PurrNet;
using System.Collections.Generic;

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
    private List<Renderer> activeAvatars = new List<Renderer>();
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

        // Set up avatar indicators for current players in the elevator
        OnPlayersChanged.AddListener(UpdateAvatars);
        for (int i = 0; i < NetworkManager.main.playerCount; i++)
        {
            Renderer updatingAvatar = avatars[i];
            updatingAvatar.gameObject.SetActive(true);
            activeAvatars.Add(updatingAvatar);
        }
        NetworkManager.main.onPlayerJoined += (player, isReconnect, asServer) =>
        {
            Debug.LogError($"Player Joined. Current Player Count: {NetworkManager.main.playerCount}");
            Renderer updatingAvatar = avatars[NetworkManager.main.playerCount - 1];
            updatingAvatar.gameObject.SetActive(true);
            activeAvatars.Add(updatingAvatar);
        };
        NetworkManager.main.onPlayerLeft += (player, asServer) =>
        {
            Debug.LogError($"Player Left. Current Player Count: {NetworkManager.main.playerCount}");
            Renderer updatingAvatar = avatars[NetworkManager.main.playerCount];
            updatingAvatar.gameObject.SetActive(false);
            activeAvatars.Remove(updatingAvatar);
        };
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
    private void UpdateAvatars(bool isReady)
    {
        for (int i = 0; i < activeAvatars.Count; i++)
        {
            if (i < playersInArea.Count) activeAvatars[i].material = avatarGreenMat;
            else activeAvatars[i].material = avatarRedMat;
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