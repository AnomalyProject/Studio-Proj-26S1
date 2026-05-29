using PurrNet;
using UnityEngine;


public class PlayerLeanAnimation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform affectedBone;// Bone to rotate
    [SerializeField] private CameraLean cameraLean;

    [Header("Settings")]
    [SerializeField] private float maxAngle = 15f;
    [SerializeField] private float lerpSpeed = 8f;

    private float currentLean;
    private SyncVar<int> effectedLean = new SyncVar<int>(0 , ownerAuth: true) ;

    private void LateUpdate()
    { // LateUpdate so not be confused with animator



        if(isOwner)
            {
            if (cameraLean != null)
            {
                effectedLean.value = (int)Mathf.Clamp(cameraLean.LeanAmount, -1f, 1f);
            }
            return;
        }
            forceLean(effectedLean.value);
        
    }

   private void forceLean (float targetLean)
    {
       // Debug.Log($"Target Lean: {targetLean} | Current Lean: {currentLean}");
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * lerpSpeed);
        float angle = currentLean * maxAngle;
        Vector3 currentEuler = affectedBone.localEulerAngles;
        //Rotation to Z
        affectedBone.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, angle);
    }
}
