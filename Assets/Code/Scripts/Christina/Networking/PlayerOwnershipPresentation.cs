using UnityEngine;
using PurrNet;
using UnityEngine.InputSystem;

public class PlayerOwnershipPresentation : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private FPSCameraController cameraController;
    [SerializeField] private GameObject bodyVisuals;
    [SerializeField] private GameObject nameplateVisuals;

    protected override void OnSpawned(bool asServer)
    {
        
        if (!asServer) ApplyOwnershipState();
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        Debug.Log($"[Ownership] OnOwnerChanged fired. Old={oldOwner}, New={newOwner}, asServer={asServer}, isOwner={isOwner}");
       if (!asServer) ApplyOwnershipState();
    }

    private void ApplyOwnershipState()
    {
        bool local = isOwner;
        Debug.Log($"[Ownership] ApplyOwnershipState called. isOwner={local}");
        if (playerCamera) playerCamera.enabled = local;
        if (playerAudioListener) playerAudioListener.enabled = local;
        if (playerInput) playerInput.enabled = local;
        if (cameraController) cameraController.enabled = local;
        if (bodyVisuals) bodyVisuals.SetActive(!local);
        if (nameplateVisuals) nameplateVisuals.SetActive(!local);
    }
}
