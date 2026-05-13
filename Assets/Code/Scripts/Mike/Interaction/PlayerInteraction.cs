using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine;
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
    public static Material outlineMaterial;

    private static List<Renderer> renderers = new List<Renderer>();
    private static List<Mesh> meshes = new List<Mesh>();

    private void Awake()
    {
        playerBody = GetComponent<PlayerBody>();
        interactionSystem = new InteractionSystem<PlayerBody>(playerBody);
        interactionSystem.OnFocusedInteractable += ShowOutline;
        interactionSystem.OnInteractableLostFocus += HideOutline;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += (stateChange) => ResetOutline();
#endif
    }
    private void OnDisable()
    {
        interactionSystem.OnFocusedInteractable -= ShowOutline;
        interactionSystem.OnInteractableLostFocus -= HideOutline;
    }

    private void Start() => InvokeRepeating(nameof(PerformScan), 0f, tickRate);

    public void InteractFocused(InputAction.CallbackContext ctx)
    {
        if(ctx.started)
        {
            if (currentInteractionTask != null && !currentInteractionTask.IsCompleted) return;
            currentInteractionTask = interactionSystem.TryInteractFocused();
        }
    }

    private void PerformScan()
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

    #region Outline
    private void ShowOutline(IInteractable<PlayerBody> interactable)
    {
        ResetOutline();
        StopAllCoroutines();
        StartCoroutine(FadeOutline(interactable, true));
    }
    private void HideOutline(IInteractable<PlayerBody> interactable) => StartCoroutine(FadeOutline(interactable, false));
    private static IEnumerator FadeOutline(IInteractable<PlayerBody> interactable, bool show)
    {
        if (interactable == null || interactable is LevelExitPoint) yield break;

        if (outlineMaterial == null)
        {
            outlineLayer = LayerMask.NameToLayer("Outlined");
            outlineMaterial = Resources.Load<Material>("InteractionSystem/Outline");
            outlineMaterial.color = new Color(outlineMaterial.color.r, outlineMaterial.color.g, outlineMaterial.color.b, 0f);
        }

        MonoBehaviour component = interactable as MonoBehaviour;
        if (component == null) yield break;

        // Get all renderers that aren't attatched to text objects
        renderers = component.gameObject.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TextMeshPro>() == null).ToList();

        // Get all meshes on renderers
        meshes.AddRange(from renderer in renderers let filter = renderer.GetComponent<MeshFilter>() where filter != null && !meshes.Contains(filter.mesh) select filter.mesh);
        meshes.AddRange(from renderer in renderers where renderer is SkinnedMeshRenderer && !meshes.Contains(((SkinnedMeshRenderer)renderer).sharedMesh) select ((SkinnedMeshRenderer)renderer).sharedMesh);
        foreach (Mesh mesh in meshes) SmoothNormals(mesh);

        // Outline them
        if (show) foreach (Renderer renderer in renderers) renderer.gameObject.layer = outlineLayer;

        // Animate towards the correct direction
        Color outlineColor = outlineMaterial.color;
        while (outlineColor.a < 1f && show || outlineColor.a > 0f && !show)
        {
            outlineColor.a += (show ? 1 : -1) * OUTLINE_FADE_SPEED * Time.unscaledDeltaTime;
            outlineMaterial.color = outlineColor;
            yield return null;
        }
        // Snap to desired values
        outlineColor.a += show ? 1 : 0;
        outlineMaterial.color = outlineColor;

        // Reset layers
        if (!show) ResetOutline();
    }
    private static void ResetOutline()
    {
        if (renderers.Count == 0) return;
        foreach (Renderer renderer in renderers) if (renderer != null) renderer.gameObject.layer = defaultLayer;
        renderers.Clear();

        foreach (Mesh mesh in meshes) mesh.RecalculateNormals();
        meshes.Clear();

        outlineMaterial.color = new Color(1, 1, 1, 0);
    }
    private static void SmoothNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Dictionary<Vector3, List<int>> groups = new Dictionary<Vector3, List<int>>();

        // Group vertices by position
        for (int i = 0; i < vertices.Length; i++)
        {
            if (!groups.TryGetValue(vertices[i], out List<int> list))
            {
                list = new List<int>();
                groups.Add(vertices[i], list);
            }
            list.Add(i);
        }

        // Average normals per group
        Vector3[] smoothNormals = new Vector3[normals.Length];
        foreach (List<int> group in groups.Values)
        {
            Vector3 avg = Vector3.zero;
            foreach (int index in group) avg += normals[index];
            avg.Normalize();
            foreach (int index in group) smoothNormals[index] = avg;
        }

        mesh.normals = smoothNormals;
    }
    #endregion
}
