using PurrNet;
using UnityEngine;


public class PlayerLeanAnimation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform affectedBone;// Bone to rotate
    [SerializeField] private CameraLean cameraLean;
    [SerializeField] private FPSCameraController cameraController;

    [Header("Settings")]
    [SerializeField] private float maxAngle = 15f;
    [SerializeField] private float lerpSpeed = 8f;
    [Header("Vertical Settings")]
    [SerializeField] private float maxVerticalAngle = 35f;
    [SerializeField] private float lerpVerticalSpeed = 8f;

    private float currentLean;
    private SyncVar<int> effectedLean = new SyncVar<int>(0 , ownerAuth: true) ;

    private float currentPitchVisual;
    private SyncVar<float> syncedPitch = new SyncVar<float>(0f, ownerAuth: true);

    private void LateUpdate()
    { // LateUpdate so not be confused with animator

        if(isOwner)
        {
            if (cameraLean != null)
            {
                effectedLean.value = (int)Mathf.Clamp(cameraLean.LeanAmount, -1f, 1f);
            }
            if (cameraController != null)
            {
                syncedPitch.value = Mathf.Clamp(cameraController.CurrentPitch / maxVerticalAngle, -1f, 1f);
            }
                return;
        }
            forceLean(effectedLean.value, syncedPitch.value);
        
    }

   private void forceLean (float targetLean , float targetPitch)
    {
       // Debug.Log($"Target Lean: {targetLean} | Current Lean: {currentLean}");
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * lerpSpeed);
        currentPitchVisual = Mathf.Lerp(currentPitchVisual, targetPitch, Time.deltaTime * lerpVerticalSpeed);
        float angle = currentLean * maxAngle;
        float pitchAngle = currentPitchVisual * maxVerticalAngle;
        Vector3 currentEuler = affectedBone.localEulerAngles;
        //Rotation to Z
        affectedBone.localRotation = Quaternion.Euler(pitchAngle, currentEuler.y, angle);
    }
}
