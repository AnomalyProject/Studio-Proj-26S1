using PurrNet;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ChalkCanister : PlayerItem, IInteractable<PlayerBody>
{
    [Header("Invisible Settings")]
    [SerializeField] private float invisibleTimer = 15.0f;

    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] ParticleSystem particles;
    
    [Header("Events")]
    public UnityEvent OnUsed;
    
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        return Task.FromResult(!interactor.Invis.IsInvis);
    }
    
    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        if (interactor == null) return false;


        if (audioSource && audioSource.clip) audioSource.Play();
        if(particles) particles.Play();
            
        int wait = Mathf.CeilToInt(audioSource.clip.length * 1000);
        await Task.Delay(wait);

        RequestActivation_ServerRpc(interactor);
        OnUsed?.Invoke();
        
        return true;
    }

    [ServerRpc] private void RequestActivation_ServerRpc(PlayerBody interactor) => interactor.Invis.StartInvisTimer(invisibleTimer);
}