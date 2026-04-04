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
    [SerializeField] private FPSController fpsController;
    [SerializeField] private CameraLean cameraLean;
    [SerializeField] private PlayerInteraction playerInteraction;

    private void Start()
    {
        if (IsSoloMode())
        {
            ApplyOwnershipState(true);
        }
    }
    
    protected override void OnSpawned(bool asServer)
    {
        if (IsSoloMode())
        {
            ApplyOwnershipState(true);
            return;
        }
        
        if (!asServer)
        {
            bool local = owner.HasValue && owner == localPlayer;
            ApplyOwnershipState(local);
        }
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        if (IsSoloMode())
        {
            ApplyOwnershipState(true);
            return;
        }
        
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
        if (fpsController) fpsController.enabled = local;
        if (cameraLean) cameraLean.enabled = local;
        if (playerInteraction) playerInteraction.enabled = local;
        if (bodyVisuals) bodyVisuals.SetActive(!local);
        if (nameplateVisuals) nameplateVisuals.SetActive(!local);
        
        if (fpsController) fpsController.IsLocalPlayer = local;
        if (cameraLean) cameraLean.IsLocalPlayer = local;
    }

    private bool IsSoloMode()
    {
        return SessionModeManager.Instance != null &&
               SessionModeManager.Instance.CurrentMode == SessionMode.Solo;
    }

}