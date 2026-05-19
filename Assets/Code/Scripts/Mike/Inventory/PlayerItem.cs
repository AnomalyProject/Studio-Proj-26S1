using PurrNet;
using UnityEngine;

public class PlayerItem : NetworkBehaviour
{
    [SerializeField, HideInInspector] private MeshRenderer[] renderers;
    [SerializeField, HideInInspector] private Canvas[] canvases;
    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (!isOwner)
        {
            // Hide Visuals on other clients
            foreach (MeshRenderer rend in renderers) rend.enabled = false;

            // Hide UI on other clients
            foreach (Canvas canvas in canvases) canvas.enabled = false;
        }
    }

    [ContextMenu("Validate Renderers")]
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        renderers = GetComponentsInChildren<MeshRenderer>();
        canvases = GetComponentsInChildren<Canvas>();
    }
}