using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads raw input from Unity's New Input System and exposes clean,
/// typed properties for FPSController and FPSCameraController to consume.
///
/// Requires: a PlayerInput component on the same GameObject configured with
/// the "IA_PlayerInputActions" Input Actions asset.
///
/// IMPORTANT — PlayerInput setup:
///   Behaviour -> "Invoke Unity Events"
///   Then wire each action to the matching public method below via the Inspector:
///     Player/Move    → OnMove
///     Player/Look    → OnLook
///     Player/Sprint  → OnSprint
///     Player/Crouch  → OnCrouch
///
/// Action Map : Player
///   Move       → Value  → Vector2
///   Look       → Value  → Vector2
///   Sprint     → Button (Hold)
///   Crouch     → Button (Toggle – handled here in code)
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class FPSInputHandler : MonoBehaviour
{
    /// <summary>WASD / Left-stick movement, normalized.</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>Mouse delta / Right-stick look, raw.</summary>
    public Vector2 LookInput { get; private set; }

    /// <summary>True while Sprint is held.</summary>
    public bool SprintHeld { get; private set; }

    /// <summary>
    /// True for exactly one frame when Crouch is pressed.
    /// Consumed by FPSController to toggle crouch state.
    /// </summary>
    public bool CrouchTriggered { get; private set; }

    private void LateUpdate()
    {
        // Reset one-frame flags after all other components have read them
        CrouchTriggered = false;
        LookInput = Vector2.zero;   // Look is delta-based; clear each frame
    }

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
        if (ctx.performed)
            CrouchTriggered = true;
    }
}