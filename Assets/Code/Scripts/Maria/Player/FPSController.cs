using UnityEngine.InputSystem;
using UnityEngine;

/// <summary>
/// First Person Character Controller using Unity's New Input System.
/// Attach to the Player root GameObject with a CharacterController component.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Physics")]
    [SerializeField] private float gravity = -19.62f;  // 2× real for snappier feel
    [SerializeField] private float groundCheckRadius = 0.28f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Camera Reference")]
    [Tooltip("Assign the Camera root child (not the Camera itself).")]
    [SerializeField] private Transform cameraHolder;
    #endregion

    #region Public Accessors
    //Public Accessors(for other systems, e.g.audio, UI)
    public bool IsLocalPlayer { get; set; }
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public bool IsGrounded => isGrounded;
    public bool IsMoving => moveInput.sqrMagnitude > 0;
    public float CurrentSpeed => isCrouching ? crouchSpeed : (sprintHeld ? sprintSpeed : walkSpeed);
    public float SpeedBoostTimeRemaining => _speedBoostTimeRemaining;
    public Vector2 MoveInput => moveInput;
    #endregion

    #region Private Fields
    private CharacterController character;

    private Vector2 moveInput;
    private bool sprintHeld;

    // Physics state
    private Vector3 velocity; // world-space velocity (gravity accumulation)
    private bool isGrounded;

    // Crouch state
    private bool isCrouching = false;
    private bool isSprinting = false;
    private float targetHeight;
    private float targetCameraLocalY;
    
    // Cached camera holder standing Y so crouch offset is relative
    private float cameraStandingLocalY;

    // Speed boost multiplier
    private float speedBoostMultiplier = 1f;
    private const float maxSpeedBoostMultiplier = 1.5f;
    private float _speedBoostTimeRemaining = 0f;
    private FOVController FOVController;
    //animator stun
    public bool IsStunned { get; set; }

    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        character = GetComponent<CharacterController>();
        FOVController = GetComponentInChildren<FOVController>(true);

        // Store default height values
        character.height = standingHeight;
        targetHeight = standingHeight;
        cameraStandingLocalY = cameraHolder != null ? cameraHolder.localPosition.y : standingHeight * 0.85f;
        targetCameraLocalY = cameraStandingLocalY;
    }

    private void Update()
    {
        if (!IsLocalPlayer) return;
       // if (IsStunned) return;
        HandleGroundCheck();
        ApplyMovement();
        HandleGravity();
        SmoothCrouchTransition();
        //Debug.Log("Player Moving:" + IsMoving);

    }
    #endregion

    #region Ground Detection
    private void HandleGroundCheck()
    {
        // Sphere at the bottom of the CharacterController
        isGrounded = Physics.CheckSphere(transform.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);

        // Reset downward velocity when grounded so we don't accumulate
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }
    #endregion

    #region Crouch Handling
    public void HandleCrouch(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        isCrouching = !isCrouching;
        Debug.Log("Crouch toggled: " + isCrouching);

        if (isCrouching)
        {
            targetHeight = crouchHeight;
            targetCameraLocalY = cameraStandingLocalY * (crouchHeight / standingHeight);
        }
        else
        {
            // Only stand if there is room above
            if (CanStandUp())
            {
                targetHeight = standingHeight;
                targetCameraLocalY = cameraStandingLocalY;
            }
            else
            {
                // Stay crouched – cancel the toggle
                isCrouching = true;
            }
        }
    }

    private void SmoothCrouchTransition()
    {
        // Smoothly resize the CharacterController
        character.height = Mathf.Lerp(character.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Keep the controller centred (adjust center Y)
        character.center = new Vector3(0f, character.height * 0.5f, 0f);

        // Smoothly move the camera holder
        if (cameraHolder == null) return;

        Vector3 camPos = cameraHolder.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraLocalY, crouchTransitionSpeed * Time.deltaTime);

        cameraHolder.localPosition = camPos;
    }

    private bool CanStandUp()
    {
        // Cast upward from current position to check for overhead obstacles
     
        float distance = standingHeight - character.height;

        Vector3 origin = transform.position + Vector3.up * character.height;

        return !Physics.SphereCast(
          origin,
          character.radius * 0.35f,
          Vector3.up,
          out _,
          distance
        );
    }
    #endregion

    #region Movement
        public void HandleMovement(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void HandleSprint(InputAction.CallbackContext ctx)
    {
        sprintHeld = ctx.performed;
    }
    private void ApplyMovement()
    {
        isSprinting = sprintHeld && moveInput.y > 0f && !isCrouching;

        float speed;
        if (isCrouching) speed = crouchSpeed;
        else if (isSprinting) speed = sprintSpeed;
        else speed = walkSpeed;

        speed *= speedBoostMultiplier;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        character.Move(move * (speed * Time.deltaTime));
    }
    #endregion

    #region Speed Boost
    public void ApplySpeedBoost(float multiplierAdditive, float duration)
    {
        if (multiplierAdditive <= 0) return;

        FOVController?.SetSpeedBoost(true); // Increase FOV

        speedBoostMultiplier = Mathf.Min(speedBoostMultiplier + multiplierAdditive, maxSpeedBoostMultiplier);
        _speedBoostTimeRemaining += duration;

        // Cancel any pending reset and restart with the updated total time
        CancelInvoke(nameof(RevertSpeedBoost));
        Invoke(nameof(RevertSpeedBoost), _speedBoostTimeRemaining);
    }
    private void RevertSpeedBoost()
    {
        FOVController?.SetSpeedBoost(false); // Reset FOV

        speedBoostMultiplier = 1f;
        _speedBoostTimeRemaining = 0;
    }
    #endregion

    #region Gravity
    private void HandleGravity()
    {
        if(!character.isGrounded) velocity.y += gravity * Time.deltaTime;
        else velocity.y = 0f;

        character.Move(velocity * Time.deltaTime);
    }
    #endregion
}