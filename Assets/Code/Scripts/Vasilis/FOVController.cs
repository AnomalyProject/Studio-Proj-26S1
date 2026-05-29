using UnityEngine.InputSystem;
using UnityEngine;

public class FOVController : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Zoom Settings")]
    [SerializeField] private Camera[] affectedCameras;

    [Header("FOV Configuration")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float speedBoostFOV = 90f;
    private float baseFOV;

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
        if (affectedCameras.Length == 0)
        {
            affectedCameras = GetComponentsInChildren<Camera>();
        }

        baseFOV = defaultFOV;
        targetFOV = baseFOV;

        foreach (var camera in affectedCameras)
        {
            camera.fieldOfView = baseFOV;
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
        targetFOV = isZooming ? (baseFOV * zoomScale) : baseFOV;
    }

    private void ApplySmoothZoom()
    {
        if (affectedCameras.Length == 0) return;
        foreach (var camera in affectedCameras)
        {
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, zoomTransitionSpeed * Time.deltaTime);
        }
    }

    public void SetSpeedBoost(bool isActive) => baseFOV = isActive ? speedBoostFOV : defaultFOV;
    #endregion
}