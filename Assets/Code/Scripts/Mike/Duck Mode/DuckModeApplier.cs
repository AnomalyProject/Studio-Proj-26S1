using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

public class DuckModeApplier : NetworkBehaviour
{
    [SerializeField] private GameObject[] duckHeads;
    private SyncVar<int> activeHeadIndex = new(0, ownerAuth: false);
    private GameObject activeHead;

    private void Awake()
    {
        foreach (var head in duckHeads) head.SetActive(false);
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        DuckMode.OnModeToggled += OnModeToggled;

        if (!asServer) RequestSync_ServerRpc();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DuckMode.OnModeToggled -= OnModeToggled;
    }

    [ServerRpc(requireOwnership: false)] private void RequestSync_ServerRpc(RPCInfo info = default) => SyncState_TargetRpc(info.sender, DuckMode.modeActive);
    [TargetRpc] private void SyncState_TargetRpc(PlayerID player, bool active) => OnModeToggled(active);

    private void OnModeToggled(bool modeActive)
    {
        if (modeActive)
        {
            if (isServer) activeHeadIndex.value = Random.Range(0, duckHeads.Length);
            activeHead = duckHeads[activeHeadIndex.value];
        }
        if (activeHead) activeHead.SetActive(modeActive);

        Debug.Log("Duck Mode active: " + modeActive);
    }

    public void ToggleDuckMode(InputAction.CallbackContext ctx)
    {
        if (ctx.started) DuckMode.ToggleMode_ServerRpc();
    }
}