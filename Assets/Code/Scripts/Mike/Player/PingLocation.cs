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
            //return;
        }

        //if (!SteamIdentity.TryGetLocalSteamID(out ulong ownerSteamID)) return;

        //SessionData currentSession = SessionManager.Instance?.CurrentSession;
        //PlayerSessionInfo? playerInfo = currentSession?.GetPlayer(ownerSteamID);

        //if (playerInfo.HasValue) pingColor = playerInfo.Value.GetPlayerColor();
    }

    public void CreatePing(InputAction.CallbackContext ctx)
    {
        if (!ctx.started || !isOwner) return;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, pingMaxDistance, pingLayer))
        {
            CreatePing_ServerRpc(hit.point); //removed arg pingColor
        }
    }

    [ServerRpc] private void CreatePing_ServerRpc(Vector3 location) //removed arg pingColor
    {
        DestroyCurrentPing_Server();

        Color colour = PlayerColour.GetColor(0);

        if (owner.HasValue)
        {
            ulong ownerSteamID = (ulong)owner.Value.id;
            SessionData currentSession = SessionManager.Instance?.CurrentSession;
            PlayerSessionInfo? playerInfo = currentSession?.GetPlayer(ownerSteamID);

            if (playerInfo.HasValue)
            {
                colour = playerInfo.Value.GetPlayerColor();
            }
        }

        currentPing = Instantiate(pingPrefab, location, Quaternion.identity);
        currentPing.SetColor_Observers(colour);
        InvokeOnPingLocation_Observers();

        if(pingDuration > 0) Destroy(currentPing, pingDuration);
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