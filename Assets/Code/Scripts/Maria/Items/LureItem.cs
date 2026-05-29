using PurrNet;
using System.Collections;
using UnityEngine;

/// <summary>
/// Placeable lure that pulses noise and audio at a fixed interval until its duration expires,
/// then destroys itself. Inherits emission logic from <see cref="NoiseEmitter"/>.
///
/// Activation flow (server-authoritative):
///   OnSpawned  → Activate() if server
///   Activate() → InvokeRepeating(Pulse, pulseInterval) + Invoke(StopLure, lureDuration)
///   Pulse()    → Emit_Server(noiseRadius) — audio replicated to all observers via ObserversRpc
///   StopLure() → CancelInvoke + Destroy(gameObject)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LureItem : NoiseEmitter
{
    #region Inspector
    [Header("Noise")]
    [Tooltip("World-space radius in which IAlertable entities will be alerted.")]
    [SerializeField] private float noiseRadius = 8f;

    [Header("Timing")]
    [Tooltip("Seconds between each noise pulse")]
    [SerializeField] private float pulseInterval = 1.5f;

    [Tooltip("Seconds the lure stays active before destroying itself.")]
    [SerializeField] private float lureDuration = 10f;

    [Header("Visuals")]
    [Tooltip("LED object to pulse in sync with noise emission.")]
    [SerializeField] private GameObject blinkingSphere;
    [SerializeField] private Light blinkingLight;
    [SerializeField] private float lightDimSpeed = 5f;
    private Material blinkingMat;
    private int EMISSION_COLOR_ID = Shader.PropertyToID("_EmissionColor");
    #endregion

    private void Start() => blinkingMat = blinkingSphere.GetComponent<MeshRenderer>().material;

    // Gradually dim the blinking light's emission color back to black over time
    private void Update()
    {
        Color currentLightColor = blinkingMat.GetColor(EMISSION_COLOR_ID);
        blinkingMat.SetColor(EMISSION_COLOR_ID, currentLightColor * Mathf.Exp(-lightDimSpeed * Time.deltaTime));
        blinkingLight.color = currentLightColor;
    }

    #region Network Lifecycle

    /// <summary>
    /// Starts the lure on the server only. Clients receive audio purely through
    /// the ObserversRpc in <see cref="NoiseEmitter.Emit_Server"/>.
    /// </summary>
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer) Activate();
    }
    #endregion

    #region Private Logic

    /// <summary>
    /// Starts the pulse loop and the self-destruct countdown.
    /// CancelInvoke() first ensures no duplicate chains if called more than once.
    /// </summary>
    private void Activate()
    {
        if (!isServer) return;

        CancelInvoke();
        InvokeRepeating(nameof(Pulse), 0f, pulseInterval);
        StartCoroutine(StopLure());
    }

    /// <summary>
    /// Emits noise at the full configured radius. No ratio applied — the lure
    /// always broadcasts at its full Inspector-configured radius.
    /// Audio is replicated to all observers automatically by <see cref="NoiseEmitter.Emit_Server"/>.
    /// </summary>
    private void Pulse()
    {
        blinkingMat.SetColor(EMISSION_COLOR_ID, Color.red);
        Emit_Server(noiseRadius, atIndex: 0);
    }

    /// <summary>Cancels all invocations and destroys this networked GameObject.</summary>
    private IEnumerator StopLure()
    {
        if (!isServer) yield break;
        yield return new WaitForSeconds(lureDuration);
        CancelInvoke();

        // Shutdown sound
        Emit_Server(noiseRadius, atIndex: 1);
        yield return new WaitForSeconds(audioClips[1].length);

        Destroy(gameObject);
    }

    private void OnDrawGizmos() => Gizmos.DrawWireSphere(transform.position, noiseRadius);
    #endregion
}