using PurrNet;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPinging : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Ray for ping from the centre of the cam. Might need a crosshair to make aiming for pings easier.")]
    public Transform playerCamera;

    [Tooltip("Visual of the ping.")]
    public GameObject pingPrefab;

    [Tooltip("Where the player can ping.")]
    public LayerMask pingableLayers;

    [Header("Settings")]
    public float pingDuration = 10f;
    public float maxPingDistance = 150f;

    private GameObject currentPingVisual;
    private Coroutine pingTimeoutCoroutine;

    private InputAction pingIA;

    private void Awake()
    {
        pingIA = InputBridge.Actions.Pinging.UsePing;
    }
    void Update()
    {
        if (!isController) return;

        if(pingIA.WasPressedThisFrame())
        {
            TryPing();
        }
    }

    private void TryPing()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxPingDistance, pingableLayers))
        {
            SendPingToServer(hit.point, hit.normal);
        }
    }

    [ServerRpc]
    private void SendPingToServer(Vector3 position, Vector3 normal)
    {
        ShowPingToClient(position, normal);
    }


    /// <summary>
    /// Displays a ping visual at the specified position and orientation for all observing clients.
    /// </summary>
    /// <remarks>This method is intended to be called on the server and will synchronize the ping visual to
    /// all clients observing the relevant object. The ping visual will be shown for a limited duration before being
    /// automatically hidden.</remarks>
    /// <param name="position">The world position where the ping visual should be displayed.</param>
    /// <param name="normal">The normal vector indicating the orientation of the ping visual.</param>
    [ObserversRpc]
    private void ShowPingToClient(Vector3 position, Vector3 normal)
    {
        if (currentPingVisual == null)
        {
            currentPingVisual = Instantiate(pingPrefab);
        }

        currentPingVisual.transform.position = position;

        currentPingVisual.transform.rotation = Quaternion.LookRotation(normal);
        currentPingVisual.SetActive(true);

        if (pingTimeoutCoroutine != null)
        {
            StopCoroutine(pingTimeoutCoroutine);
        }
        pingTimeoutCoroutine = StartCoroutine(PingTimeoutRoutine());
    }

    private IEnumerator PingTimeoutRoutine()
    {
        yield return new WaitForSeconds(pingDuration);

        if (currentPingVisual != null)
        {
            currentPingVisual.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (currentPingVisual != null)
        {
            Destroy(currentPingVisual);
        }
    }
}