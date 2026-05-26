using PurrNet;
using UnityEngine;


public class PlayerLeanAnimation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform affectedBone;// Bone to rotate
    private CameraLean cameraLean;

    [Header("Settings")]
    [SerializeField] private float maxAngle = 15f;
    [SerializeField] private float lerpSpeed = 8f;

    private float currentLean;
    private Quaternion initialRotation;
    private SyncVar<float> effectedLean = new SyncVar<float>(ownerAuth:true);
    // Save default rotation
    private void Awake()
    {
        if (affectedBone != null)
            initialRotation = affectedBone.localRotation;
        
    }

    private void LateUpdate()
    { // LateUpdate so not be confused with animator

        if (isOwner)
        {
            if (cameraLean == null)
            {
                
                
                    cameraLean = GetComponentInChildren<CameraLean>();
                
            }

            if (cameraLean != null)
            {
                effectedLean.value = Mathf.Clamp(cameraLean.LeanAmount, -1f, 1f);
            }

        }
        forceLean(effectedLean.value);
    }

   private void forceLean (float targetLean)
    {
       // Debug.Log($"Target Lean: {targetLean} | Current Lean: {currentLean}");
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * lerpSpeed);
        float angle = currentLean * maxAngle;
        //Rotation to Z
        affectedBone.localRotation = initialRotation * Quaternion.Euler (0f,0f, angle);
    }
}
