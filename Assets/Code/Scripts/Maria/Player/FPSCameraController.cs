using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-Person Camera Controller.
/// Handles vertical (pitch) rotation of the Camera and horizontal (yaw)
/// Attach to the CameraHolder GameObject not the Camera itself.
/// </summary>
public class FPSCameraController : MonoBehaviour
{
    #region Inspector Configuration

    [Header("Pitch Clamp (degrees)")]
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;
    
    [Header("Clamp Values")]
    [SerializeField] private float mouseDegreesPerPixel = 0.08f;
    [SerializeField] private float gamepadDegreesPerSecond = 260f;
    [SerializeField] private float maxYawDegreesPerFrame = 35f;
    [SerializeField] private float maxPitchDegreesPerFrame = 25f;
    [SerializeField] private float maxLookDeltaTime = 1f / 30f;

    [Header("References")]
    [Tooltip("The Player root transform (rotates on Y axis).")]
    [SerializeField] private Transform playerBody;
    Vector2 lookInput;
    #endregion

    #region Private Fields
    private float currentPitch = 0f;   // accumulated vertical rotation
    private bool lookFromMouse;
    private bool suppressNextLookFrame;
    #endregion

    #region Public Accessors
    public float CurrentPitch => currentPitch;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (playerBody == null)
            Debug.LogError("[FPSCameraController] Player Body reference is not assigned.");
    }
    private void Start()
    {
        //LockCursor();
    }
    
    private void OnEnable()
    {
        InputBridge.OnContextChanged += HandleInputContextChanged;
        Application.focusChanged += HandleFocusChanged;
    }

    private void OnDisable()
    {
        InputBridge.OnContextChanged -= HandleInputContextChanged;
        Application.focusChanged -= HandleFocusChanged;
    }

    private void Update()
    {
        // clearing cached input to avoid applying old look data while menus/chat/UI are active.
        if (InputBridge.CurrentContext != InputBridge.InputContext.Player)
        {
            lookInput = Vector2.zero;
            return;
        }

        // cursor locking, focus regain, and input-map switches produces one bad mouse delta.
        // dropping one frame is much less noticeable than allowing a sudden camera snap
        if (suppressNextLookFrame)
        {
            suppressNextLookFrame = false;
            lookInput = Vector2.zero;
            return;
        }

        UpdateLook();
    }

    #endregion

    #region Look
    public void ApplyLook(InputAction.CallbackContext ctx)
    {
        // clearing cached look when the action is canceled so a disabled input
        // cannot leave stale movemebt applied on later frames. 
        if (ctx.canceled)
        {
            lookInput = Vector2.zero;
            return;
        }
        lookInput = ctx.ReadValue<Vector2>();
        
        // remember the device so mouse deltas and gamepad stick input are scaled correctly.
        lookFromMouse = ctx.control?.device is Mouse;
    }

    private void UpdateLook()
    {
        if (playerBody == null) return;

        Vector2 lookDegrees;

        if (lookFromMouse)
        {
            // mouse delta is already frame-relative, so we do not need to multiply it by deltaTime.
            // doing that makes spiked frames dangerous because both mouse delta and deltaTime can spike.
            float mouseSensitivity = mouseDegreesPerPixel * InputBridge.Sensitivity;
            lookDegrees = lookInput * mouseSensitivity;
        }
        else
        {
            // gamepad stick input is a rate, so it should be scaled by time
            // clamp the deltaTime so a performance hitch cannot cause a large camera jump.
            float dt = Mathf.Min(Time.unscaledDeltaTime, maxLookDeltaTime);
            float gamepadSensitivity = gamepadDegreesPerSecond * InputBridge.Sensitivity * dt;
            lookDegrees = lookInput * gamepadSensitivity;
        }

        lookDegrees.x = Mathf.Clamp(lookDegrees.x, -maxYawDegreesPerFrame, maxYawDegreesPerFrame);
        lookDegrees.y = Mathf.Clamp(lookDegrees.y, -maxPitchDegreesPerFrame, maxPitchDegreesPerFrame);

        float yaw = lookDegrees.x * (InputBridge.invertX ? -1 : 1);
        playerBody.Rotate(Vector3.up, yaw, Space.World);

        float pitch = lookDegrees.y * (InputBridge.invertY ? -1 : 1);
        currentPitch = Mathf.Clamp(currentPitch - pitch, pitchMin, pitchMax);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
    
    // Menus and input context changes can relock/recenter the cursor so
    // ignore the next look frame so that cursor movement does not snap the camera
    private void HandleInputContextChanged(InputBridge.InputContext context)
    {
        lookInput = Vector2.zero;
        suppressNextLookFrame = true;
    }
    // (having worked with web games) alt-tabbing or refocusing the game can create one bad mouse delta!
    // Ignore the next look frame so the camera does not suddenly jump
    private void HandleFocusChanged(bool hasFocus)
    {
        lookInput = Vector2.zero;
        suppressNextLookFrame = true;
    }
    #endregion

    #region Cursor Control
    //private static void LockCursor()
    //{
    //    Cursor.lockState = CursorLockMode.Locked;
    //    Cursor.visible = false;
    //}

    //// Call from a pause / menu system when you need to release the cursor
    //public static void UnlockCursor()
    //{
    //    Cursor.lockState = CursorLockMode.None;
    //    Cursor.visible = true;
    //}
    #endregion
}