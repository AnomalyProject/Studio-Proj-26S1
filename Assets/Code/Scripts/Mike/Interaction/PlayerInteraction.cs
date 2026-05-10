using UnityEngine.InputSystem;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using TMPro;

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

    [Header("Outlines")]
    public const float OUTLINE_FADE_SPEED = 10f;
    public static LayerMask outlineLayer;
    public static LayerMask defaultLayer = 0;
    public static Material objectOutlineMaterial;
    public static Material viewOutlineMaterial;

    private static List<Renderer> renderers;

    private void Awake()
    {
        playerBody = GetComponent<PlayerBody>();
        interactionSystem = new InteractionSystem<PlayerBody>(playerBody);
        interactionSystem.OnFocusedInteractable += ShowOutline;
        interactionSystem.OnInteractableLostFocus += HideOutline;
    }
    private void OnDisable()
    {
        interactionSystem.OnFocusedInteractable -= ShowOutline;
        interactionSystem.OnInteractableLostFocus -= HideOutline;
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

    private void ShowOutline(IInteractable<PlayerBody> interactable)
    {
        ResetOutline();
        StopAllCoroutines();
        StartCoroutine(FadeOutline(interactable, true));
    }
    private void HideOutline(IInteractable<PlayerBody> interactable) => StartCoroutine(FadeOutline(interactable, false));
    private static IEnumerator FadeOutline(IInteractable<PlayerBody> interactable, bool show)
    {
        if (objectOutlineMaterial == null)
        {
            outlineLayer = LayerMask.NameToLayer("Outlined");
            objectOutlineMaterial = Resources.Load<Material>("InteractionSystem/Object Outline");
            viewOutlineMaterial = Resources.Load<Material>("InteractionSystem/View Outline");
            objectOutlineMaterial.color = new Color(objectOutlineMaterial.color.r, objectOutlineMaterial.color.g, objectOutlineMaterial.color.b, 0f);
        }

        // Get all renderers that aren't attatched to text objects
        renderers = (interactable as MonoBehaviour)?.gameObject.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TextMeshPro>() == null).ToList();

        // Outline them
        if (show) foreach (Renderer renderer in renderers) renderer.gameObject.layer = outlineLayer;

        // Animate towards the correct direction
        Color outlineColor = objectOutlineMaterial.color;
        while (outlineColor.a < 1f && show || outlineColor.a > 0f && !show)
        {
            outlineColor.a += (show ? 1 : -1) * OUTLINE_FADE_SPEED * Time.unscaledDeltaTime;
            objectOutlineMaterial.color = outlineColor;
            viewOutlineMaterial.color = outlineColor;
            yield return null;
        }
        // Snap to desired values
        outlineColor.a += show ? 1 : 0;
        objectOutlineMaterial.color = outlineColor;
        viewOutlineMaterial.color = outlineColor;

        // Reset layers
        if (!show) ResetOutline();
    }
    private static void ResetOutline()
    {
        if (renderers == null) return;
        foreach (Renderer renderer in renderers) renderer.gameObject.layer = defaultLayer;
        renderers = null;

        objectOutlineMaterial.color = new Color(1, 1, 1, 0);
        viewOutlineMaterial.color = objectOutlineMaterial.color;
    }
}
