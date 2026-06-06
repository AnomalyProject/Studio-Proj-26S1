using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class CollectibleInteractable : MonoBehaviour, IInteractable<PlayerBody>
{
    [SerializeField] private CollectibleSO collectibleData;
    [SerializeField] UnityEvent onCollected;

    public Task<bool> CanInteract(PlayerBody interactor)
    {
        bool hasCollectible = RefrenceManager.CurrentSave.collectiblesGathered.Contains(collectibleData.ID);
        return Task.FromResult(!hasCollectible);
    }

    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        RefrenceManager.CurrentSave.collectiblesGathered.Add(collectibleData.ID);
        onCollected.Invoke();
        await Task.Run(() => SaveSystem.QuickSave(RefrenceManager.CurrentSave));
        return true;
    }
}