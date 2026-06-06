using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class CollectibleInteractable : MonoBehaviour, IInteractable<PlayerBody>
{
    [SerializeField] private CollectibleSO collectibleData;
    [SerializeField] private UnityEvent onCollected;
    [SerializeField, Tooltip("Called in awake if already gathered or after succesfully collecting via interaction.")] private UnityEvent onDisableEffects;

    public CollectibleSO CollectibleData => collectibleData;
    public bool CollectibleGathered => RefrenceManager.CurrentSave.collectiblesGathered.Contains(collectibleData.ID);

    private void Awake()
    {
        if (CollectibleGathered) onDisableEffects?.Invoke();
    }

    public Task<bool> CanInteract(PlayerBody interactor) => Task.FromResult(!CollectibleGathered);
    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        RefrenceManager.CurrentSave.collectiblesGathered.Add(collectibleData.ID);
        onCollected.Invoke();
        onDisableEffects?.Invoke();
        await Task.Run(() => SaveSystem.QuickSave(RefrenceManager.CurrentSave));
        return true;
    }
}