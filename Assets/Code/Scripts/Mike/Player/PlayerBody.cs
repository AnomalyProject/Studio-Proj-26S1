using System;
using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBody : NetworkBehaviour
{
    [Header("Body Components")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private FPSController movement;
    [SerializeField] private FPSCameraController cameraController;
    [SerializeField] private PlayerInteraction interaction;
    [SerializeField] private GameObject bodyVisuals;
    
    [Header("Local Player")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;
    [SerializeField] private CameraLean playerCameraLean;
    [SerializeField] private GameObject nameplateVisuals;
    
    public Inventory Inventory => playerInventory.Inventory;
    public FPSController Movement => movement;
    public FPSCameraController CameraController => cameraController;
    public PlayerInteraction Interaction => interaction;
    public PlayerID? OwnerPlayerID => owner;

    public static event Action<PlayerBody> OnLocalPlayerSpawned;
    public static event Action<PlayerBody> OnLocalPlayerDespawned;
    public static PlayerBody localPlayerBody;

    // guards the local player callbacks because ownership can be applied from both OnSpawned and OnOwnerChanged. 
    // So the issue is that PlayrBody can register two times. We want to prevent that.  
    private bool isLocalPlayerRegistered;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

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
}