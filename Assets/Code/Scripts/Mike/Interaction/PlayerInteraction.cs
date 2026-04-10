using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerBody))]
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

    PlayerBody playerBody;
    InteractionSystem<PlayerBody> interactionSystem;

    void Awake()
    {
        playerBody = GetComponent<PlayerBody>();
        interactionSystem = new InteractionSystem<PlayerBody>(playerBody);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating(nameof(PerformScan), 0f, tickRate);
    }

    public void InteractFocused(InputAction.CallbackContext ctx)
    {
        //if (!CanUseLocalInteraction()) return;
        
        if(ctx.started)
        interactionSystem.TryInteractFocused();
    }

    void PerformScan()
    {
        //if (!CanUseLocalInteraction()) return;
        
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

    /*private bool CanUseLocalInteraction()
    {
        if (SessionModeManager.Instance != null && SessionModeManager.Instance.CurrentMode == SessionMode.Solo)
        {
            return true;
        }
        
        return isOwner;
    }*/
}
