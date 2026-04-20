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
   
        if (Mouse.current != null)
        {
            isZooming = Mouse.current.rightButton.isPressed;
        }

        CalculateTargetFOV();
        ApplySmoothZoom();
    }
    #endregion

    #region Zoom Logic
    private void CalculateTargetFOV()
    {
        // When zooming multiply the default FOV by our scale slider

        targetFOV = isZooming ? (defaultFOV * zoomScale) : defaultFOV;
    }

    private void ApplySmoothZoom()
    {
        if (playerCamera == null) return;

        // Smooth out the camera transition of zoom
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomTransitionSpeed * Time.deltaTime);
    }
    #endregion
}