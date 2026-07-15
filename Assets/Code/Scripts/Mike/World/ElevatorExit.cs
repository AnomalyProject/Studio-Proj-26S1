using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using PurrNet;
using System;
using System.Collections;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class ElevatorExit : LevelExitPoint
{
    [Serializable] protected struct InidicationMessage
    {
        public Color indicationColor;
        public Sprite indicationSprite;
    }

    [SerializeField, Header("Animation Events")] private UnityEvent OnFullyClosed;
    [SerializeField] private UnityEvent OnFullyOpened, OnStartOpen;
    [SerializeField] private Renderer[] anomalyColorIndicators;
    [SerializeField] private Image anomalyImage;
    [SerializeField] private InidicationMessage anomalyMessage, safeMessage;

    [SerializeField] private Renderer[] avatars;
    [SerializeField] private GameObject ornament;
    [SerializeField] private Material avatarGreenMat, avatarRedMat;

    [SerializeField] private bool openOnStart;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Animator anim;

    [Header("Audio")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    protected override void Awake()
    {
        base.Awake();
        bHasAnomaly.onChanged += UpdateAnomalyIndicators;

        UpdateAvatarCount(0, null);
        SessionEvents.OnPlayerJoined += UpdateAvatarCount;
        SessionEvents.OnPlayerLeft += UpdateAvatarCount;
        OnPlayersChanged.AddListener(UpdateAvatarColors);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SessionEvents.OnPlayerJoined -= UpdateAvatarCount;
        SessionEvents.OnPlayerLeft -= UpdateAvatarCount;
        OnPlayersChanged.RemoveListener(UpdateAvatarColors);
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (asServer) return;

        if (anim == null) anim = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        UpdateAnomalyIndicators(bHasAnomaly);

        if (openOnStart && gameObject.activeInHierarchy) StartCoroutine(InitialOpenDoors());
    }
    private IEnumerator InitialOpenDoors()
    {
        if(GameManager.Instance != null && GameManager.Instance.AnomalyManager != null)
        {
            // wait for the map to spawn
            yield return new WaitUntil(() => GameManager.Instance.AnomalyManager.ActiveMap != null);
        }

        OpenDoors();
    }
    private void UpdateAvatarColors(bool isReady)
    {
        for (int i = 0; i < NetworkManager.main.playerCount; i++) avatars[i].material = i < playersInArea.Count ? avatarGreenMat : avatarRedMat;
    }
    private void UpdateAvatarCount(ulong steamID, string displayName)
    {
        ornament.SetActive(true);
        foreach (Renderer avatar in avatars) avatar.gameObject.SetActive(false);
        if (networkManager.playerCount <= 1) return;
        for (int i = 0; i < NetworkManager.main.playerCount; i++)
        {
            avatars[i].gameObject.SetActive(true);
            ornament.SetActive(false);
        }
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
        Debug.Log($"{StackTraceUtility.ExtractStackTrace()}");

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