using System;
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
    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivityX = 0.15f;
    [SerializeField] private float mouseSensitivityY = 0.15f;

    [Header("Pitch Clamp (degrees)")]
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;

    [Header("References")]
    [Tooltip("The Player root transform (rotates on Y axis).")]
    [SerializeField] private Transform playerBody;
    Vector2 lookInput;
    #endregion

    #region Private Fields
    private float currentPitch = 0f;   // accumulated vertical rotation
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (playerBody == null)
            Debug.LogError("[FPSCameraController] Player Body reference is not assigned.");
    }
    private void Start()
    {
        LockCursor();
    }

    void Update() => UpdateLook();

    #endregion

    #region Look
    public void ApplyLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    void UpdateLook()
    {
        if (playerBody == null) return;

        // Horizontal -> rotate the player body (yaw)
        playerBody.Rotate(Vector3.up, lookInput.x * mouseSensitivityX, Space.World);

        // Vertical -> rotate the camera holder (pitch), clamped
        currentPitch -= lookInput.y * mouseSensitivityY;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
    #endregion

    #region Cursor Control
    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Call from a pause / menu system when you need to release the cursor
    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    #endregion
}