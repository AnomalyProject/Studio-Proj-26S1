using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads raw input from Unity's New Input System and exposes clean,
/// typed properties for FPSController and FPSCameraController to consume.
///
/// Requires: a PlayerInput component on the same GameObject configured with
/// the "IA_PlayerInputActions" Input Actions asset.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class FPSInputHandler : MonoBehaviour
{
    #region Exposed Properties
    public Vector2 MoveInput { get; private set; }

    public Vector2 LookInput { get; private set; }

    public bool SprintHeld { get; private set; }

    public bool CrouchTriggered { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void LateUpdate()
    {
        // Reset one-frame flags after all other components have read them
        CrouchTriggered = false;
        LookInput = Vector2.zero;   // Look is delta-based; clear each frame
    }
    #endregion

    #region Input Callbacks
    // Input System Callbacks (wired via PlayerInput -> Invoke Unity Events)
    // These are public so they appear in the Inspector event dropdown.

    public void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
        Debug.Log("Move: " + MoveInput);
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        LookInput = ctx.ReadValue<Vector2>();
        Debug.Log("Look: " + LookInput);
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        SprintHeld = ctx.performed;
    }

    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            CrouchTriggered = true;
    }
    #endregion
}