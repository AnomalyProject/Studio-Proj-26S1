using UnityEngine;
using PurrNet;
using Steamworks;

public class PlayerBodyColour : NetworkBehaviour
{
    [SerializeField] private Renderer suitRenderer;
    [SerializeField] private int materialIndex = 0;

    private Material runtimeMat;

    [ObserversRpc(bufferLast: true)]
    public void SetBodyColour(int colourIndex)
    {
        ApplyColour(PlayerColour.GetColor(colourIndex));
    }

    private void ApplyColour(Color color)
    {
        if (suitRenderer == null) return;

        if (runtimeMat == null)
        {
            Material[] mats = suitRenderer.materials;
            runtimeMat = mats[materialIndex];
            suitRenderer.materials = mats;
        }

        runtimeMat.color = color;
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (!SteamManager.Initialized) return;
        if (!owner.HasValue) return;
        ulong ownerSteamID = (ulong)owner.Value.id;

        SessionData currentSession = SessionManager.Instance?.CurrentSession;
        PlayerSessionInfo? playerInfo = currentSession?.GetPlayer(ownerSteamID);
        if (playerInfo.HasValue)
        {
            ApplyColour(playerInfo.Value.GetPlayerColor());
        }
    }
}