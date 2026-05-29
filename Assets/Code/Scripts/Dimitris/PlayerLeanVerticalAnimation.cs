using PurrNet;
using UnityEngine;

public class PlayerLeanVerticalAnimation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform affectedBoneVertical; // Bone to rotate
    [SerializeField] private FPSCameraController cameraController;

    [SerializeField] private float maxVerticalAngle = 35f;
    [SerializeField] private float lerVerticalSpeed = 8f;

    private Quaternion initialRotationVertical;
    private float currentPitchVisual;

    private SyncVar<float> syncedPitch = new SyncVar<float>(0f , ownerAuth:true);
    // Save default rotation
    private void Awake()
    {
        if (affectedBoneVertical != null)
            initialRotationVertical = affectedBoneVertical.localRotation;
    }

    private void LateUpdate()
    {
        // LateUpdate so not be confused with animator
        if (affectedBoneVertical == null)
            return;

        if (isOwner && cameraController != null)
        {
            float normalizedPitch = Mathf.Clamp(cameraController.CurrentPitch / maxVerticalAngle, -1f, 1f);
            syncedPitch.value = normalizedPitch;
            return;
        }
        ApplyPitch(syncedPitch.value);
    }

    private void ApplyPitch(float targetPitch)
    {
        currentPitchVisual = Mathf.Lerp(currentPitchVisual, targetPitch, Time.deltaTime * lerVerticalSpeed);
        float pitchAngle = currentPitchVisual * maxVerticalAngle;
        Vector3 currentEuler = affectedBoneVertical.localEulerAngles;

        affectedBoneVertical.localRotation = Quaternion.Euler(pitchAngle, currentEuler.y, currentEuler.z); //affect X axis

    }
}
