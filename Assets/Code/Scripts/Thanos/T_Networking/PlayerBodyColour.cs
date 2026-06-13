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
        if (suitRenderer == null)
        {
            Debug.LogError("[BodyColour] suitRenderer is NULL");
            return;
        }

        if (runtimeMat == null)
        {
            Material[] mats = suitRenderer.materials;
            Debug.LogWarning($"[BodyColour] Renderer '{suitRenderer.name}' has {mats.Length} materials");
            for (int i = 0; i < mats.Length; i++)
                Debug.LogWarning($"[BodyColour] Slot {i}: {mats[i].name}, shader: {mats[i].shader.name}");

            runtimeMat = mats[materialIndex];
            suitRenderer.materials = mats;
        }

        Debug.LogWarning($"[BodyColour] Setting {runtimeMat.name} color to {color}, has _BaseColor: {runtimeMat.HasProperty("_BaseColor")}, has _Color: {runtimeMat.HasProperty("_Color")}");
        runtimeMat.color = color;
        Debug.LogWarning($"[BodyColour] After set, color is now: {runtimeMat.color}");
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