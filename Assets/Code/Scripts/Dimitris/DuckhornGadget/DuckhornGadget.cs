using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class DuckhornGadget : PlayerItem , IInteractable<PlayerBody>
{
    [Header("Stun Settings")]
    [SerializeField] private float stunRadius = 8f;
    [SerializeField] private float durationSeconds = 5f;
    [SerializeField] private LayerMask stunLayerMask;

    [Header("Feedback")]
    [SerializeField] private AudioClip useClip;

    public UnityEvent onUsed;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public Task<bool> CanInteract(PlayerBody interactor)
    {
        return Task.FromResult(true);
    }

    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        // Execute usage logic on server
        Use_ServerRpc();

        float waitSeconds;

        if (useClip != null)
        {
            waitSeconds = useClip.length;
        }
        else
        {
            waitSeconds = 0f;
        }
        int waitMilliseconds = Mathf.RoundToInt(waitSeconds * 1000f);
        // Wait until feedback audio finishes
        if (waitMilliseconds > 0)
            await Task.Delay(waitMilliseconds);

        return true;
    }

    [ServerRpc]
    private void Use_ServerRpc()
    {
        ApplyStun_Server();
        PlayUsedFeedback_ObserversRpc();
    }

    private void ApplyStun_Server()
    {
        if (!isServer) return;

        Collider[] hits = Physics.OverlapSphere( transform.position, stunRadius,stunLayerMask );
        // Prevent duplicate stun targets
        HashSet<IStunnable> affectedTargets = new();

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out IStunnable stunnable))
            {
                affectedTargets.Add(stunnable);
                continue;
            }

            if (hit.attachedRigidbody != null && hit.attachedRigidbody.TryGetComponent(out stunnable))
            {
                affectedTargets.Add(stunnable);
            }
        }

        foreach (IStunnable target in affectedTargets)
        {
            target.Stun(durationSeconds);
        }
    }

    [ObserversRpc]
    private void PlayUsedFeedback_ObserversRpc()
    {
        // Trigger visuals/audio for all clients , event call
        onUsed?.Invoke();

        if (audioSource != null && useClip != null)
            audioSource.PlayOneShot(useClip);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Shows stun range in editor
        Gizmos.DrawWireSphere(transform.position, stunRadius);
    }
#endif
}

