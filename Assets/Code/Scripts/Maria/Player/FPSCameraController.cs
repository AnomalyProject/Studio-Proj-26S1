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
    [SerializeField, Min(1)] float sensitivityMultiplier = 20f;

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
    public float CurrentPitch => currentPitch;

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

    void Update() => UpdateLook(Time.deltaTime * sensitivityMultiplier);

    #endregion

    #region Look
    public void ApplyLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    void UpdateLook(float delta)
    {
        if (playerBody == null) return;

        // Horizontal -> rotate the player body (yaw)
        float yaw = lookInput.x * (InputBridge.invertX ? -1 : 1) * InputBridge.Sensitivity * delta;
        playerBody.Rotate(Vector3.up, yaw, Space.World);

        // Vertical -> rotate the camera holder (pitch), clamped
        float pitch = lookInput.y * (InputBridge.invertY ? -1 : 1) * InputBridge.Sensitivity * delta;
        currentPitch -= pitch;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
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