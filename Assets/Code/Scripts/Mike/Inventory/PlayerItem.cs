using PurrNet;
using UnityEngine;
using UnityEngine.Events;

public class PlayerItem : NetworkBehaviour
{
    [SerializeField, HideInInspector] private MeshRenderer[] renderers;
    [SerializeField, HideInInspector] private Canvas[] canvases;
    [SerializeField] private UnityEvent onLocalSpawn;
    [SerializeField, Tooltip("Optional Entry Scriptable Object. Registers on Creation.")] private AlmanacEntrySO almanacEntry;

    private IReadOnlyItemStack boundStack;
    private int boundSlot;
    private PlayerInventory playerInventory;

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
        else
        {
            if (almanacEntry != null) AlmanacDiscovery.Discover(almanacEntry);
            onLocalSpawn.Invoke();
        }
    }

    [ContextMenu("Validate Renderers")]
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        renderers = GetComponentsInChildren<MeshRenderer>();
        canvases = GetComponentsInChildren<Canvas>();
    }

    [ServerRpc(runLocally: true)] public void BindTo(PlayerInventory inventory, IReadOnlyItemStack stack, int slot)
    {
        if (inventory == null) return;

        this.playerInventory = inventory;
        boundStack = stack;
        boundSlot = slot;
    }

    protected T GetMeta<T>(string key, T fallback = default) => boundStack != null ? boundStack.GetMeta(key, fallback) : fallback;
    protected bool TryGetMeta<T>(string key, out T value)
    {
        if (boundStack != null) return boundStack.TryGetMeta(key, out value);
        value = default;
        return false;
    }
    protected void SetMeta_Server(string key, object value)
    {
        if (!isServer) return;
        playerInventory.Inventory.SetMetadata(boundSlot, key, value);
    }
}