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
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject bodyVisuals;

    public Inventory Inventory => playerInventory.Inventory;
    public FPSController Movement => movement;
    public FPSCameraController CameraController => cameraController;
    public PlayerInteraction Interaction => interaction;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer) return;
        if (!TryApplyOwnership(isOwner)) return;
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        base.OnOwnerChanged(oldOwner, newOwner, asServer);

        if (asServer) return;

        bool local = newOwner.HasValue && newOwner == localPlayer;
        if (!TryApplyOwnership(local)) return;
    }

    private bool TryApplyOwnership(bool local)
    {
        Debug.Log($"[Ownership] ApplyOwnershipState: local={local}");
        
        if(local) playerInput.ActivateInput();
        else playerInput.DeactivateInput();

        if (bodyVisuals) bodyVisuals.SetActive(!local);
        interaction.enabled = local;

        return local;
    }
}