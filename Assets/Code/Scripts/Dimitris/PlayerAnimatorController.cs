using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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
    //Lerp variables
    private float currentMoveX;
    private float currentMoveY;

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
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isOwner) return;
            if (fpsController == null || animator == null) return;
        
        
        
            
            Vector2 moveInput = fpsController.MoveInput;
        currentMoveX = Mathf.Lerp(currentMoveX, moveInput.x, Time.deltaTime * 20f);
        currentMoveY = Mathf.Lerp(currentMoveY, moveInput.y, Time.deltaTime * 20f);
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
