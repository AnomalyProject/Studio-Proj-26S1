using UnityEngine;

/// <summary>
/// First Person Character Controller using Unity's New Input System.
/// Handles body movement (walk, sprint, crouch) driven by FPSInputHandler.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(FPSInputHandler))]
public class FPSController : MonoBehaviour
{
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

    private CharacterController character;
    private FPSInputHandler input;

    private Vector3 velocity; // world-space velocity (gravity accumulation)
    private bool isGrounded;
    private bool isCrouching;

    private float targetHeight;
    private float targetCameraLocalY;

    // Cached camera holder standing Y so crouch offset is relative
    private float cameraStandingLocalY;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        input = GetComponent<FPSInputHandler>();

        // Store default height values
        character.height = standingHeight;
        targetHeight = standingHeight;
        cameraStandingLocalY = cameraHolder != null ? cameraHolder.localPosition.y : standingHeight * 0.85f;
        targetCameraLocalY = cameraStandingLocalY;
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleCrouchToggle();
        HandleMovement();
        HandleGravity();
        SmoothCrouchTransition();
    }

    private void HandleGroundCheck()
    {
        // Sphere at the bottom of the CharacterController
        Vector3 sphereOrigin = transform.position + Vector3.up * (character.radius);
        isGrounded = Physics.CheckSphere(sphereOrigin, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);

        // Reset downward velocity when grounded so we don't accumulate
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }

    private void HandleCrouchToggle()
    {
        if (!input.CrouchTriggered) return;

        isCrouching = !isCrouching;

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

    private bool CanStandUp()
    {
        // Cast upward from current position to check for overhead obstacles
        float castDistance = standingHeight - crouchHeight;
        return !Physics.SphereCast(transform.position, groundCheckRadius, Vector3.up, out _, castDistance, groundLayers, QueryTriggerInteraction.Ignore);

    }

    private void HandleMovement()
    {
        Vector2 inputDir = input.MoveInput;

        // Determine speed
        float speed;
        if (isCrouching)
            speed = crouchSpeed;
        else if (input.SprintHeld && inputDir.y > 0f) // Sprint only when moving forward
            speed = sprintSpeed;
        else
            speed = walkSpeed;

        // Build move vector in local space -> convert to world space
        Vector3 move = transform.right * inputDir.x
                     + transform.forward * inputDir.y;

        character.Move(move * (speed * Time.deltaTime));
    }

    private void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        character.Move(velocity * Time.deltaTime);
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

    // Public Accessors (for other systems, e.g. audio, UI)

    //public bool IsCrouching => isCrouching;
    //public bool IsSprinting => input.SprintHeld && !isCrouching;
    //public bool IsGrounded => isGrounded;
    //public float CurrentSpeed => isCrouching ? crouchSpeed : (input.SprintHeld ? sprintSpeed : walkSpeed);

}