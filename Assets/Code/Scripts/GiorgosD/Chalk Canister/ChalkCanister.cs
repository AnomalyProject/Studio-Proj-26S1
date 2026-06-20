using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ChalkCanister : PlayerItem, IInteractable<PlayerBody>
{
    [Header("References")]
    [SerializeField] private AudioSource audio;
    [SerializeField] ParticleSystem particles;
    
    [Header("Events")]
    public UnityEvent OnUsed;
    
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        return Task.FromResult(true);
    }

    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        if (audio == null && audio.clip == null && particles == null) return false;

        audio.Play();
        //particles.Play();
            
        int wait = Mathf.CeilToInt(audio.clip.length * 1000);
        await Task.Delay(wait);
        
        OnUsed?.Invoke();
        
        interactor.StartInvisTimer();
        
        return true;
    }
}
