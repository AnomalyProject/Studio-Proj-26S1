using PurrNet;
using UnityEngine;

/// <summary>
/// Owns footstep timing, audio, and noise emission for the local player.
/// Inherits from <see cref="NoiseEmitter"/> and reads movement state exclusively
/// through <see cref="PlayerBody"/> so <see cref="FPSController"/> is never
/// coupled to this class directly.
///
/// On each step tick:
///   - Plays footstep audio locally via <see cref="NoiseEmitter.PlayAudioLocal"/>
///   - Sends a <see cref="RequestEmission"/> ServerRpc → <see cref="NoiseEmitter.EmitWithRatio_Server"/>
///     → Physics.OverlapSphere → IAlertable.Alert
/// </summary>
[RequireComponent(typeof(PlayerBody))]
public class PlayerNoise : NoiseEmitter
{
    #region Inspector
    [Header("Noise Radius")]
    [SerializeField] float sprintNoiseRadius = 6f;
    [SerializeField] float walkNoiseRadius = 3f;
    [SerializeField] float crouchNoiseRadius = 0.8f;

    [Header("Radius Ratio")]
    [Tooltip("Scales the effective emit radius without changing the base values above.\n" +
             "1.0 = full radius  |  0.5 = half radius  |  0.0 = silent")]
    [SerializeField, Range(0f, 1f)] float radiusRatio = 1f;

    [Header("Step Intervals (seconds)")]
    [SerializeField] float sprintStepInterval = .35f;
    [SerializeField] float walkStepInterval = .55f;
    [SerializeField] float crouchStepInterval = .75f;

    [Header("Footstep Audio")]
    [SerializeField] float sprintVolume = 1f;
    [SerializeField] float walkVolume = .6f;
    [SerializeField] float crouchVolume = .2f;
    #endregion

    #region Private Fields
    PlayerBody playerBody;
    float stepTimer;
    #endregion

    #region Unity Lifecycle
    protected override void Awake() 
    { 
        base.Awake();
        playerBody = GetComponent<PlayerBody>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer) return;
        stepTimer = GetCurrentInterval();
    }

    private void Update()
    {     
        if (!IsMoving()) return;

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0)
        {
            OnStep();
            stepTimer = GetCurrentInterval();
        }
    }
    #endregion

    #region Step Logic

    /// <summary>
    /// Fired on each step tick.
    /// Plays footstep audio locally (client-side only) then asks the server
    /// to perform the noise emission and alert any IAlertable entities in range.
    /// </summary>
    void OnStep()
    {
        PlayAudioLocal(GetCurrentVolume());
        RequestEmission(GetCurrentRadius());
    }
    #endregion

    #region Private Logic

    /// <summary>
    /// ServerRpc — forwards the emission request to the server so the
    /// authoritative overlap query runs there, not on the client.
    /// </summary>
    [ServerRpc] private void RequestEmission(float radius)
    {
        EmitWithRatio_Server(radius, radiusRatio, 0);
    }
    #endregion

    #region Movement State Helpers

    /// <summary>Returns true when the player has directional input.</summary>
    bool IsMoving() => playerBody.Movement.IsMoving;

    /// <summary>Returns the noise radius matching the current movement state.</summary>
    private float GetCurrentRadius()
    {
        if (playerBody.Movement.IsSprinting) return sprintNoiseRadius;
        if (playerBody.Movement.IsCrouching) return crouchNoiseRadius;
        return walkNoiseRadius;
    }

    /// <summary>Returns the step interval matching the current movement state.</summary>
    private float GetCurrentInterval()
    {
        if (playerBody.Movement.IsSprinting) return sprintStepInterval;
        if (playerBody.Movement.IsCrouching) return crouchStepInterval;
        return walkStepInterval;
    }

    /// <summary>Returns the footstep volume matching the current movement state.</summary>
    private float GetCurrentVolume()
    {
        if (playerBody.Movement.IsSprinting) return sprintVolume;
        if (playerBody.Movement.IsCrouching) return crouchVolume;
        return walkVolume;
    }
    #endregion
}