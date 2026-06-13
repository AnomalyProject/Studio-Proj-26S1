using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using System.Collections;

public class PlayerAnimatorController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private FPSController fpsController;
    [SerializeField] private NetworkAnimator animator;
    [SerializeField] private RuntimeAnimatorController controller;

    private CharacterController characterController;
    
    //set all Hashes we are gonna use for better performance
    private int moveXHash;
    private int moveYHash;
    private int crouchHash;
    private int sprintHash;
    //shove hashes
    private int shoveXHash;
    private int shoveYHash;
    private int shovedHash;
    private int getUpHash;
    private int isStunnedHash;
    private int pushHash;
    //Lerp variables
    private float currentMoveX;
    private float currentMoveY;
    //shove locomotion
    private float externalMoveX;
    private float externalMoveY;
    
    [SerializeField] private float lerpSpeed = 20f;

    private void Awake()
    {
        if (fpsController==null)
            fpsController = GetComponent<FPSController>();
        characterController = GetComponent<CharacterController>();
        if (animator == null)
        {
            animator = GetComponentInChildren<NetworkAnimator>();
            if (animator == null)
                Debug.LogError("Animator not found");
        }
        if (animator != null && controller != null)
           // animator.runtimeAnimatorController = controller;
        // Cache Animator parameter hashes for better performance
        moveXHash = Animator.StringToHash("MoveX");
        moveYHash = Animator.StringToHash("MoveY");
        crouchHash = Animator.StringToHash("isCrouching");
        sprintHash = Animator.StringToHash("isSprinting");
        //parameters of shove added here
        shoveXHash = Animator.StringToHash("ShoveX");
        shoveYHash = Animator.StringToHash("ShoveY");
        shovedHash = Animator.StringToHash("Shoved");
       // getUpHash = Animator.StringToHash("GetUp");
        isStunnedHash = Animator.StringToHash("IsStunned");
        pushHash = Animator.StringToHash("Push");
        
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isOwner) return;
        if (fpsController == null || animator == null) return;
        
        
        
        
        Vector2 moveInput = fpsController.MoveInput;
        moveInput.x += externalMoveX;
        moveInput.y += externalMoveY;
        moveInput = Vector2.ClampMagnitude(moveInput,1f);
        currentMoveX = Mathf.Lerp(currentMoveX, moveInput.x, Time.deltaTime * lerpSpeed);
        currentMoveY = Mathf.Lerp(currentMoveY, moveInput.y, Time.deltaTime * lerpSpeed);
        UpdateAnimator(currentMoveX, currentMoveY, fpsController.IsCrouching , fpsController.IsSprinting);

        
        
    }
   // [ObserversRpc]
    private void UpdateAnimator(float moveX , float moveY , bool isCrouching , bool isSprinting)
    {
       // if (isOwner) return;//cause of first person owner doesnt have to see full body animator
        // Sync Animator state with input
        animator.SetFloat(moveXHash , moveX);
        animator.SetFloat (moveYHash , moveY);
        animator.SetBool(crouchHash, isCrouching);
        animator.SetBool(sprintHash , isSprinting);
       
    }
    public void ApplyShove(Vector3 force)
    {
       
        Vector3 localDir = transform.InverseTransformDirection(force.normalized);

        animator.SetFloat(shoveXHash, localDir.x);
        animator.SetFloat(shoveYHash, localDir.z);
       

        animator.SetTrigger(shovedHash);
        
    }
    public void ApplyShoveLocomotion(Vector3 force)
    {
        Vector3 local = transform.InverseTransformDirection(force);
        externalMoveX = Mathf.Clamp(local.x / 5f, -1f, 1f);
        externalMoveY = Mathf.Clamp(local.z / 5f, -1f, 1f);
        StartCoroutine(ClearShoveLocomotion());
    }

    public void SetStunned(bool state)
    {
        animator.SetBool(isStunnedHash, state);
    }
   /* public void GetUp()
    {
        animator.SetTrigger(getUpHash);
    }*/
    public void PlayPushAnimation()
    {
       
        animator.SetTrigger(pushHash);
        
    }
    
    
    private IEnumerator ClearShoveLocomotion()
    {
        float t = 0f;
        float startX = externalMoveX;
        float startY = externalMoveY;

        while (t < 1.8)
        {
            t += Time.deltaTime;
            float k = t / 1.8f;

            externalMoveX = Mathf.Lerp(startX, 0f, k);
            externalMoveY = Mathf.Lerp(startY, 0f, k);

            yield return null;
        }

        externalMoveX = 0f;
        externalMoveY = 0f;
    }
    /*  // Since moveInput is private in FPSController
      // Converts world velocity to local space and clamps it for Animator parameters
      private Vector2 GetMoveInput()
      {
          Vector3 localVelocity = transform.InverseTransformDirection(characterController.velocity);
          float moveX = Mathf.Clamp(localVelocity.x , -1f ,  1f);
          float moveY = Mathf.Clamp(localVelocity.z, -1f, 1f);
          return new Vector2(moveX, moveY);
      } */
}
