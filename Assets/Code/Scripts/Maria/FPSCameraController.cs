using System;
using UnityEngine;

/// <summary>
/// First-Person Camera Controller.
/// Handles vertical (pitch) rotation of the Camera and horizontal (yaw)
/// rotation of the Player root, driven by FPSInputHandler.LookInput.
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
    #endregion

    #region Private Fields
    private FPSInputHandler inputHandler;
    private float currentPitch = 0f;   // accumulated vertical rotation
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Input handler lives on the Player root
        inputHandler = playerBody != null ? playerBody.GetComponent<FPSInputHandler>() : GetComponentInParent<FPSInputHandler>();

        if (inputHandler == null)
            Debug.LogError("[FPSCameraController] Could not find FPSInputHandler. " + "Ensure it is on the Player root GameObject.");
    }

    private void Update()
    {
        ApplyLook();
    }
    #endregion

    #region Look
    private void ApplyLook()
    {
        if (inputHandler == null) return;

        Vector2 look = inputHandler.LookInput;

        // Horizontal -> rotate the player body (yaw)
        playerBody.Rotate(Vector3.up, look.x * mouseSensitivityX, Space.World);

        // Vertical -> rotate the camera holder (pitch), clamped
        currentPitch -= look.y * mouseSensitivityY;
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