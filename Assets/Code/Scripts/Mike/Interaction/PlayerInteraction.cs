using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FPSController))]
public class PlayerInteraction : MonoBehaviour
{
    enum InteractionMode
    {
        Raycast,
        OverlapSphere
    }

    [Header("Configuration Settings")]
    [SerializeField] Camera playerCamera;
    [SerializeField] InteractionMode interactionMode;
    [SerializeField, Min(.05f)] float tickRate = 0.1f;
    [SerializeField] LayerMask scanLayer;
    [SerializeField, Min(.1f)] float scanRange = 5f;

    [Header("Debug Options")]
    [SerializeField] bool debugGizmos = true;

    FPSController playerController;
    InteractionSystem<FPSController> interactionSystem;

    void Awake()
    {
        playerController = GetComponent<FPSController>();
        interactionSystem = new InteractionSystem<FPSController>(playerController);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating(nameof(PerformScan), 0f, tickRate);
    }

    public void InteractFocused(InputAction.CallbackContext ctx)
    {
        if(ctx.started)
        interactionSystem.TryInteractFocused();
    }

    void PerformScan()
    {
        switch (interactionMode)
        {
            case InteractionMode.Raycast: interactionSystem.RaycastScan(playerCamera, scanRange, scanLayer);
                break;

            case InteractionMode.OverlapSphere: interactionSystem.OverlapSphereScan(transform, scanRange, scanLayer);
                break;
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugGizmos) return;

        Gizmos.color = Color.blue;

        switch (interactionMode)
        {
            case InteractionMode.Raycast:
                if(playerCamera) 
                    Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * scanRange);
                break;

            case InteractionMode.OverlapSphere:
                Gizmos.DrawWireSphere(transform.position, scanRange);
                break;
        }
    }
}
