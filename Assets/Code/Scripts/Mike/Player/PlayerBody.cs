using System;
using System.Collections;
using PurrNet;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class PlayerBody : NetworkBehaviour
{
    [Header("Body Components")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private FPSController movement;
    [SerializeField] private FPSCameraController cameraController;
    [SerializeField] private PlayerInteraction interaction;
    [SerializeField] private GameObject bodyVisuals;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Local Player")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;
    [SerializeField] private CameraLean playerCameraLean;
    [SerializeField] private GameObject nameplateVisuals;

    [Header("Misc")]
    [SerializeField] private AudioClip burpClip;

    [Header("Invisibility")] 
    [SerializeField] private SyncVar<bool> isInvisible;
    [SerializeField] private float invisibleTimer;
    [SerializeField] private Material invisMat;
    private Renderer playerRenderer;
    private Material[] originalMat;
    
    [SerializeField] AudioMixer mainMixer;
    [SerializeField] private float audioTransTime = 0.4f;
    private string snapshotNormal = "Normal";
    private string snapshotMuffled = "Muffled";
    
    [SerializeField] private GameObject PPInvis;
    
    public Inventory Inventory => playerInventory.Inventory;
    public FPSController Movement => movement;
    public FPSCameraController CameraController => cameraController;
    public Camera PlayerCamera => playerCamera;
    public PlayerInteraction Interaction => interaction;
    public PlayerID? OwnerPlayerID => owner;
    public AudioSource AudioSource => audioSource;
    public bool IsInvisible => isInvisible.value;

    public static event Action<PlayerBody> OnLocalPlayerSpawned;
    public static event Action<PlayerBody> OnLocalPlayerDespawned;
    public static PlayerBody localPlayerBody;

    // guards the local player callbacks because ownership can be applied from both OnSpawned and OnOwnerChanged. 
    // So the issue is that PlayrBody can register two times. We want to prevent that.  
    private bool isLocalPlayerRegistered;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (bodyVisuals != null)
        {
            playerRenderer = bodyVisuals.GetComponentInChildren<Renderer>();
            originalMat = playerRenderer.sharedMaterials;
        }
        
        if (asServer) return;
        if (!TryApplyOwnership(isOwner)) return;
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        
        if (!isLocalPlayerRegistered) return;
        
        localPlayerBody = null;
        OnLocalPlayerDespawned?.Invoke(this);
        isLocalPlayerRegistered = false;
    }

    //protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    //{
    //    base.OnOwnerChanged(oldOwner, newOwner, asServer);
    //
    //    if (asServer) return;
    //
    //    bool local = newOwner.HasValue && newOwner == localPlayer;
    //    if (!TryApplyOwnership(local)) return;
    //}

    [ObserversRpc(requireServer: false)] public void DoBurp(float afterSeconds)
    {
        CancelInvoke(nameof(Burp));
        Invoke(nameof(Burp), afterSeconds);
    }

    private void Burp()
    {
        if (!audioSource) return;
        audioSource.PlayOneShot(burpClip);
    }

    private bool TryApplyOwnership(bool local)
    {
        Debug.Log($"[PlayerBody:Ownership] ApplyOwnershipState: local={local}");
        
        playerCamera.enabled = local;
        playerAudioListener.enabled = local;
        cameraController.enabled = local;
        
        movement.enabled = local;
        movement.IsLocalPlayer = local;
        
        playerCameraLean.enabled = local;
        playerCameraLean.IsLocalPlayer = local;

        if (bodyVisuals) bodyVisuals.SetActive(!local);
        interaction.enabled = local;
        
        if(nameplateVisuals) nameplateVisuals.SetActive(!local);

        if (local && !isLocalPlayerRegistered)
        {
            localPlayerBody = this;
            isLocalPlayerRegistered = true;
            OnLocalPlayerSpawned?.Invoke(this);
        }

        return local;
    }
    
    public void StartInvisTimer()
    {
        if (!isServer) return;
        
        StartCoroutine(InvisTimer());
    }

    private IEnumerator InvisTimer()
    {
        isInvisible.value = true;
        ChangeMatRPC(true);
        yield return new WaitForSeconds(invisibleTimer);
        ChangeMatRPC(false);
        isInvisible.value = false;
    }

    [ServerRpc]
    private void ChangeMatRPC(bool shouldBeInvis)
    {
        if (isOwner)
        {
            if (PPInvis != null)
            {
                PPInvis.SetActive(shouldBeInvis);
            }
            
            if (mainMixer != null)
            {
                string targetSnapshot = shouldBeInvis ? snapshotMuffled : snapshotNormal;
                AudioMixerSnapshot snapshot = mainMixer.FindSnapshot(targetSnapshot);
                if (snapshot != null)
                {
                    snapshot.TransitionTo(audioTransTime);
                }
            }
        }
        else
        {
            if (playerRenderer != null && invisMat != null)
            {
                if (shouldBeInvis)
                {
                    Material[] invisMats = new  Material[originalMat.Length];
                    for (int i = 0; i < invisMats.Length; i++)
                    {
                        invisMats[i] = invisMat;
                    }
                    playerRenderer.materials = invisMats;
                }
                else
                {
                    playerRenderer.materials = originalMat;
                }
            }
        }
    }
}