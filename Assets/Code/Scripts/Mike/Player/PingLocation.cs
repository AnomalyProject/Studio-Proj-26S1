using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine;
using Steamworks;
using PurrNet;

public class PingLocation : NetworkBehaviour
{
    [SerializeField] private PingObject pingPrefab;
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(10)] private float pingMaxDistance = 100f;
    [SerializeField, Min(0)] private float pingDuration = 10f;
    [SerializeField] private LayerMask pingLayer;
    [SerializeField] AudioClip pingSFX;
    [SerializeField] private UnityEvent OnPingLocation;

    private PingObject currentPing;
    private Color pingColor = Color.lightSkyBlue;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer)
        {
            if(RefrenceManager.Instance)
            RefrenceManager.Instance.Gameplay.AnomalyManager.OnMapChanged += (_) => DestroyCurrentPing_Server();
            return;
        }

        if (!SteamIdentity.TryGetLocalSteamID(out ulong ownerSteamID)) return;

        SessionData currentSession = SessionManager.Instance?.CurrentSession;
        PlayerSessionInfo? playerInfo = currentSession?.GetPlayer(ownerSteamID);

        if (playerInfo.HasValue) pingColor = playerInfo.Value.GetPlayerColor();
    }

    public void CreatePing(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, pingMaxDistance, pingLayer))
        {
            CreatePing_ServerRpc(hit.point, pingColor);
        }
    }

    [ServerRpc] private void CreatePing_ServerRpc(Vector3 location, Color color)
    {
        DestroyCurrentPing_Server();

        currentPing = Instantiate(pingPrefab, location, Quaternion.identity);
        currentPing.SetColor_Observers(color);
        InvokeOnPingLocation_Observers();

        if(pingDuration > 0) Destroy(currentPing, pingDuration);
    }

    public void SetColor(int color)
    {
        currentPing.pingImage.color = PlayerColour.GetColor(color);
    }

    private void DestroyCurrentPing_Server()
    {
        if (!isServer) return;
        if(currentPing == null) return;
        Destroy(currentPing.gameObject);
        currentPing = null;
    }

    [ObserversRpc] private void InvokeOnPingLocation_Observers()
    {
        if(pingSFX && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(pingSFX);
        OnPingLocation?.Invoke();
    }
}