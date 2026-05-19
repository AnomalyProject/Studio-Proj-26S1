using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerZooming : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Zoom Settings")]
    [SerializeField] private Camera[] affectedCameras;

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
        if (affectedCameras.Length == 0)
        {
            affectedCameras = GetComponentsInChildren<Camera>();
        }

        targetFOV = defaultFOV;

        foreach (var camera in affectedCameras)
        {
            camera.fieldOfView = defaultFOV;
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
        if (affectedCameras.Length == 0) return;
        foreach (var camera in affectedCameras)
        {
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, zoomTransitionSpeed * Time.deltaTime);
        }
    }
    #endregion
}