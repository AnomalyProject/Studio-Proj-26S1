using PurrNet;
using UnityEngine;

/// <summary>
/// Abstract base class for all noise-emitting entities in the game.
/// Performs Physics.OverlapSphere on the server and dispatches
/// <see cref="IAlertable.Alert{NoiseEmitter}"/> to every receiver in range.
/// Audio is replicated to all observers via <see cref="EmitAudio_ObserversRpc"/>.
///
/// Subclasses own all timing and radius decisions — this class only executes the query and audio.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public abstract class NoiseEmitter : NetworkBehaviour
{
    #region Inspector
    [Header("Detection")]
    [Tooltip("Any layer whose collider should react to noise - monsters, traps, etc." +
             "Add new reactive layers here without touching code!")]
    [SerializeField] LayerMask hearableLayers;
    [SerializeField] AudioClip[] emissionNoises;
    #endregion

    #region Protected Field
    protected AudioSource audioSource { get; private set; }
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake() => audioSource = GetComponent<AudioSource>();
    #endregion

    #region Public API
    /// <summary>
    /// Server-only. Performs a sphere query at the current position and calls Alert(this)
    /// on every <see cref="IAlertable"/> found within <paramref name="radius"/>.
    /// Silently skips colliders that don't implement IAlertable.
    /// Does nothing if radius is &lt;= 0.
    /// If <paramref name="emitVolume"/> is greater than 0, replicates a random audio clip
    /// to all observers via RPC.
    /// </summary>
    /// <param name="radius">World-space sphere radius for the overlap query.</param>
    /// <param name="emitVolume">Volume at which observers hear the emission. Pass 0 to suppress audio.</param>
    public void Emit_Server(float radius, float emitVolume = 1)
    {
        if (!isServer)
        {
            Debug.LogWarning("Only the server can perform noise emitions");
            return;
        }

        if (radius <= 0) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, hearableLayers);
        //Debug.Log($"[NoiseEmitter] Emit radius={radius}, hits={hits.Length}");

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out IAlertable alertable)) alertable.Alert(this);
        }

        int audioIndex = Random.Range(0, emissionNoises.Length);

        if(emitVolume > 0)
        EmitAudio_ObserversRpc(audioIndex, emitVolume);
        // Non-IAlertable hits are intentionally ignored - no error, no log.
    }

    /// <summary>
    /// Server-only. Applies a [0, 1] multiplier to <paramref name="radius"/> then calls
    /// <see cref="Emit_Server"/>. Useful for scaling effective noise without changing base
    /// Inspector values.
    /// </summary>
    /// <param name="radius">Base radius in world units.</param>
    /// <param name="radiusRatio">Multiplier clamped to [0, 1].</param>
    /// <param name="volume">Volume passed through to <see cref="Emit_Server"/>.</param>
    public void EmitWithRatio_Server(float radius, float radiusRatio, float volume = 1)
        => Emit_Server(radius * Mathf.Clamp01(radiusRatio), volume);
    #endregion

    #region Private Logic

    /// <summary>
    /// Plays a random clip from <see cref="emissionNoises"/> locally on this client only.
    /// Used by subclasses for sounds that should not be replicated (e.g. local footsteps).
    /// </summary>
    /// <param name="volume">Playback volume.</param>
    [ObserversRpc] private void EmitAudio_ObserversRpc(int atIndex, float volume = 1) 
        => audioSource.PlayOneShot(emissionNoises[atIndex], volume);
    #endregion

    #region Protected Logic

    /// <summary>
    /// Replicated to all observers. Plays the clip at <paramref name="atIndex"/> so every
    /// client hears the emission in world space.
    /// </summary>
    /// <param name="atIndex">Index into <see cref="emissionNoises"/>.</param>
    /// <param name="volume">Playback volume.</param>
    protected void PlayAudioLocal(float volume = 1)
    {
        int audioIndex = Random.Range(0, emissionNoises.Length);
        audioSource.PlayOneShot(emissionNoises[audioIndex], volume);
    }
    #endregion
}