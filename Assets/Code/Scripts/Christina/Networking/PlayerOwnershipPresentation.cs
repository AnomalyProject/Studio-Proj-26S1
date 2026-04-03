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
        if (!asServer)
        {
            bool local = owner.HasValue && owner == localPlayer;
            ApplyOwnershipState(local);
        }
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        if (!asServer)
        {
            bool local = newOwner.HasValue && newOwner == localPlayer;
            Debug.Log($"[Ownership] OnOwnerChanged: newOwner={newOwner}, localPlayer={localPlayer}, local={local}");
            ApplyOwnershipState(local);
        }
    }

    private void ApplyOwnershipState(bool local)
    {
        Debug.Log($"[Ownership] ApplyOwnershipState: local={local}");
        if (playerCamera) playerCamera.enabled = local;
        if (playerAudioListener) playerAudioListener.enabled = local;
        if (playerInput) playerInput.enabled = local;
        if (cameraController) cameraController.enabled = local;
        if (bodyVisuals) bodyVisuals.SetActive(!local);
        if (nameplateVisuals) nameplateVisuals.SetActive(!local);
    }
}