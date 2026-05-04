using PurrNet;
using UnityEngine;

public class PlayerItem : NetworkBehaviour
{
    [SerializeField, HideInInspector] MeshRenderer[] renderers;
    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (!isOwner)
        {
            foreach(MeshRenderer rend in renderers) // Hide Visuals on other clients
            {
                rend.enabled = false;
            }
        }
    }

    [ContextMenu("Validate Renderers")]
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        renderers = GetComponentsInChildren<MeshRenderer>();
    }
}