using UnityEngine;

/// <summary>
/// First person Lean (Peek) Controller.
/// Smoothly tilts the camera left and right based on FPSInputHandler.LeanInput, which is driven by player input.
/// Attach to the CameraHolder GameObject
/// IA_PlayerInputActions asset wired via PlayerInput component → Invoke Unity Events
/// </summary>
public class CameraLean : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Lean Angle")]
    [SerializeField] private float leanAngle = 15f;

    [Header("Camera Offset")]
    [Tooltip("How far the camera shifts sideways at full lean.")]
    [SerializeField, Range(.1f, 1)] private float leanDistance = .5f;

    [Header("Smoothing")]
    [Tooltip("Higher values = faster lean transition. Set to a very high value for instant lean (no smoothing, do not recommend :)).")]
    [SerializeField] private float leanSpeed = 7f;

    [Header("References")]
    [SerializeField] private Transform playerBody;
    #endregion

    #region Private Fields
    private FPSInputHandler input;

    private float currentAngle;
    private float currentOffsetX;
    private float targetAngle;
    private float targetOffsetX;

    private Vector3 neutralLocalPosition;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        input = playerBody != null ? playerBody.GetComponent<FPSInputHandler>() : GetComponentInParent<FPSInputHandler>();

        if(input == null)
            Debug.LogError("[CameraLean] could not find FPSInputHandler component on player body or parent." + 
                "Ensure it is on the Player Root GameObject.");

        neutralLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        CalculateTargets();
        ApplyLean();
    }
    #endregion

    #region Lean
    void CalculateTargets()
    {
        // LeanInput: -1 = left, 0 = neutral, 1 = right (clamped in case both held)
        float lean = Mathf.Clamp(input.LeanInput, -1f, 1f);

        targetAngle = lean * -leanAngle; // negative = roll left, positive = roll right
        targetOffsetX = lean * leanDistance;
    }

    void ApplyLean()
    {
        // Smoothly interpolate current angle and offset towards targets
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, leanSpeed * Time.deltaTime);
        currentOffsetX = Mathf.Lerp(currentOffsetX, targetOffsetX, leanSpeed * Time.deltaTime);

        // Apply roll on top of whatever pitch FPSController has set
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, currentAngle);

        // Shift the camera holder laterally
        Vector3 pos = neutralLocalPosition;
        pos.x += currentOffsetX;
        transform.localPosition = pos;
    }
    #endregion
}