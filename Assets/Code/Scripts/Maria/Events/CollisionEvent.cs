using PurrNet;
using System;
using UnityEngine;
using UnityEngine.Events;

// <summary>
/// Fires <see cref="UnityEvent"/>s on all clients when this object collides with something.
/// Supports playing all responses at once or picking one at random, with a configurable
/// cooldown to prevent event jittering on sustained contact.
/// </summary>
public class CollisionEvent : NetworkBehaviour
{
    #region Nested Types
    [Serializable] public class CollisionResponse
    {
        public string label; // inspector-only label for readability
        public UnityEvent OnCollision;
    }
    #endregion

    #region Inspector
    [SerializeField, Tooltip("if true only one random response plays, otherwise all responses play")] 
    private bool doRandomResponse = false;

    [SerializeField] private CollisionResponse[] responses;

    [SerializeField, Min(0f), Tooltip("Minimum seconds between collision events to fire. Prevents event jittering on sustained contact.")]
    private float cooldown = 0.2f;
    #endregion

    #region State
    private float coolDownRemaining;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Fill the cooldown on spawn so the first collision frame doesn't trigger a sound.
        coolDownRemaining = cooldown;
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);

        if (asServer) return;
    }
    private void Update()
    {
        if (coolDownRemaining > 0f) coolDownRemaining -= Time.deltaTime;
    }
    private void OnCollisionEnter(Collision collision) => PerformCollision();
    #endregion

    #region Collision Handling
    /// <summary>
    /// Called on every collision enter. Server-authoritative — only the server
    /// decides whether to fire and which responses to invoke, then broadcasts
    /// to all clients via <see cref="HandleCollision_ObserversRpc"/>.
    /// </summary>
    private void PerformCollision()
    {
        if (!isServer || coolDownRemaining > 0f) return;

        coolDownRemaining = cooldown;

        if (!doRandomResponse) for (int i = 0; i < responses.Length; i++) HandleCollision_ObserversRpc(i);
        else HandleCollision_ObserversRpc(UnityEngine.Random.Range(0, responses.Length));
    }
    /// <summary>
    /// Invokes the response at the given index on all clients.
    /// </summary>
    [ObserversRpc] private void HandleCollision_ObserversRpc(int responseIndex)
    {
        responses[responseIndex].OnCollision.Invoke();
    }
    #endregion
}