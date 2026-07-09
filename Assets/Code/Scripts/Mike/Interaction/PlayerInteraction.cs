using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public static Material outlineMaterial;

    private static Dictionary<Renderer, int> rendererData = new Dictionary<Renderer, int>();
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
    private static readonly Dictionary<Mesh, Vector3[]> originalNormals = new Dictionary<Mesh, Vector3[]>();
    private static CancellationTokenSource outlineCTS;
    private sealed class MeshNormalJob
    {
        public Mesh Mesh;
        public Vector3[] Vertices;
        public Vector3[] OriginalNormals;
        public Vector3[] SmoothedNormals;
    }

    private void ShowOutline(IInteractable<PlayerBody> interactable)
    {
        ResetOutline();
        outlineCTS?.Cancel();
        outlineCTS = new CancellationTokenSource();
        _ = FadeOutlineAsync(interactable, true, outlineCTS.Token);
    }
    private void HideOutline(IInteractable<PlayerBody> interactable)
    {
        ResetOutline();
        outlineCTS?.Cancel();
        outlineCTS = new CancellationTokenSource();
        _ = FadeOutlineAsync(interactable, false, outlineCTS.Token);
    }
    private static void ResetOutline()
    {
        if (rendererData.Count == 0) return;
        foreach (KeyValuePair<Renderer, int> rd in rendererData) if (rd.Key != null) rd.Key.gameObject.layer = rd.Value;
        rendererData.Clear();

        foreach (Mesh mesh in meshes) if (originalNormals.TryGetValue(mesh, out var normals)) mesh.normals = normals;
        meshes.Clear();

        outlineMaterial.color = new Color(1, 1, 1, 0);
    }
    private static async Task FadeOutlineAsync(IInteractable<PlayerBody> interactable, bool show, CancellationToken token)
    {
        if (interactable == null || interactable is LevelExitPoint) return;

        if (outlineMaterial == null)
        {
            outlineLayer = LayerMask.NameToLayer("Outlined");
            outlineMaterial = Resources.Load<Material>("InteractionSystem/Outline");
            outlineMaterial.color = new Color(outlineMaterial.color.r, outlineMaterial.color.g, outlineMaterial.color.b, 0);
        }

        if (interactable is not MonoBehaviour component) return;

        if (show)
        {
            // Get all renderers that aren't attatched to text objects
            rendererData = component.gameObject.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TextMeshPro>() == null).ToDictionary(r => r, r => r.gameObject.layer);

            // Get all meshes on renderers
            meshes.AddRange(rendererData.Keys.Select(r => r.GetComponent<MeshFilter>()).Where(f => f != null).Select(f => f.mesh));
            meshes.AddRange(rendererData.Keys.OfType<SkinnedMeshRenderer>().Select(r => r.sharedMesh));
            meshes = meshes.Distinct().ToList();

            // Smooth them on a worker thread
            await SmoothMeshesAsync(meshes);

            // Outline them
            foreach (Renderer renderer in rendererData.Keys) renderer.gameObject.layer = outlineLayer;
        }

        // Animate towards the correct direction
        Color color = outlineMaterial.color;
        while ((show && color.a < 1f) || (!show && color.a > 0f))
        {
            color.a += (show ? 1 : -1) * OUTLINE_FADE_SPEED * Time.unscaledDeltaTime;
            outlineMaterial.color = color;
            token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
        // Snap to desired values
        color.a = show ? 1 : 0;
        outlineMaterial.color = color;

        // Reset layers
        if (!show) ResetOutline();
    }
    private static async Task SmoothMeshesAsync(List<Mesh> meshes)
    {
        List<MeshNormalJob> jobs = new List<MeshNormalJob>(meshes.Count);

        // Store original normals and prepare jobs for each mesh
        foreach (Mesh mesh in meshes)
        {
            originalNormals.TryAdd(mesh, mesh.normals);

            jobs.Add(new MeshNormalJob()
            {
                Mesh = mesh,
                Vertices = mesh.vertices,
                OriginalNormals = mesh.normals
            });
        }

        // Calculate smoothed normals for each mesh in parallel
        await Task.WhenAll(jobs.Select(job => Task.Run(() => job.SmoothedNormals = CalculateSmoothNormals(job.Vertices, job.OriginalNormals))));

        // Apply the smoothed normals back to the meshes on the main thread
        foreach (MeshNormalJob job in jobs) job.Mesh.normals = job.SmoothedNormals;
    }
    private static Vector3[] CalculateSmoothNormals(Vector3[] vertices, Vector3[] normals)
    {
        Dictionary<Vector3, List<int>> groups = new Dictionary<Vector3, List<int>>();

        // Group vertices by position
        for (int i = 0; i < vertices.Length; i++)
        {
            if (!groups.TryGetValue(vertices[i], out var list))
            {
                list = new List<int>();
                groups.Add(vertices[i], list);
            }
            list.Add(i);
        }

        // Average normals per group
        Vector3[] smoothNormals = new Vector3[normals.Length];
        foreach (var group in groups.Values)
        {
            Vector3 avg = Vector3.zero;
            foreach (int index in group) avg += normals[index];
            avg.Normalize();
            foreach (int index in group) smoothNormals[index] = avg;
        }

        return smoothNormals;
    }
    #endregion
}
