using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerZooming : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Zoom Settings")]
    [SerializeField] private Camera playerCamera;

    [Header("FOV Configuration")]
    [SerializeField] private float defaultFOV = 60f;

    [Range(0.1f, 1.0f)]
    [SerializeField] private float zoomScale = 0.5f;

    [SerializeField] private float zoomTransitionSpeed = 12f;
    #endregion

    #region Private Fields
    private float targetFOV;
    private bool isZooming;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        targetFOV = defaultFOV;

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = defaultFOV;
        }
    }

    private void Update()
    {
        CalculateTargetFOV();
        ApplySmoothZoom();
    }
    #endregion

    #region Input Action Callback (Professional Method)

    public void HandleZoom(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isZooming = true;
        }
        else if (ctx.canceled)
        {
            isZooming = false;
        }
    }
    #endregion

    #region Zoom Logic
    private void CalculateTargetFOV()
    {
        targetFOV = isZooming ? (defaultFOV * zoomScale) : defaultFOV;
    }

    private void ApplySmoothZoom()
    {
        if (playerCamera == null) return;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomTransitionSpeed * Time.deltaTime);
    }
    #endregion
}