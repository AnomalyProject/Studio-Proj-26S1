using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ChalkCanister : PlayerItem, IInteractable<PlayerBody>
{
    [Header("Settings")]
    [SerializeField, Tooltip("Lower values mean longer fade in.")] private float fadeInSpeed = 0.2f;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] ParticleSystem particles;
    private PlayerInvisibility playerInvis;
    
    [Header("Events")]
    public UnityEvent OnUsed;
    
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        return Task.FromResult(true);
    }
    
    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        if (interactor == null 
            || audioSource == null 
            || audioSource.clip == null 
            || particles == null) return false;
        
        playerInvis = interactor.GetComponent<PlayerInvisibility>();
        
        audioSource.Play();
        particles.Play();
        playerInvis.EnablePPVolume(true, 1.0f, fadeInSpeed);
            
        int wait = Mathf.CeilToInt(audioSource.clip.length * 1000);
        await Task.Delay(wait);
        
        OnUsed?.Invoke();
        
        playerInvis.StartInvisTimer();
        
        return true;
    }
}
