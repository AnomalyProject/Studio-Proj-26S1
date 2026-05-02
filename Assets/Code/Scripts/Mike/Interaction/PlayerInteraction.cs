using UnityEngine.InputSystem;
using UnityEngine;
using System.Threading.Tasks;

[RequireComponent(typeof(PlayerBody))]
public class PlayerInteraction : MonoBehaviour
{
    private enum InteractionMode
    {
        Raycast,
        OverlapSphere
    }

    [Header("Configuration Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InteractionMode interactionMode;
    [SerializeField, Min(.05f)] private float tickRate = 0.1f;
    [SerializeField] private LayerMask scanLayer;
    [SerializeField, Min(.1f)] private float scanRange = 5f;

    [Header("Debug Options")]
    [SerializeField] private bool debugGizmos = true;

    private PlayerBody playerBody;
    public InteractionSystem<PlayerBody> interactionSystem { get; private set; }
    private Task currentInteractionTask;

    private void Awake()
    {
        playerBody = GetComponent<PlayerBody>();
        interactionSystem = new InteractionSystem<PlayerBody>(playerBody);
    }

    private void Start()
    {
        InvokeRepeating(nameof(PerformScan), 0f, tickRate);
    }

    public void InteractFocused(InputAction.CallbackContext ctx)
    {
        //if (!CanUseLocalInteraction()) return;
        
        if(ctx.started)
        {
            if (currentInteractionTask != null && !currentInteractionTask.IsCompleted) return;
            currentInteractionTask = interactionSystem.TryInteractFocused();
        }
    }

    private void PerformScan()
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
