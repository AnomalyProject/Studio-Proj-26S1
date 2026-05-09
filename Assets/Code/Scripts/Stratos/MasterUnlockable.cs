using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using UnityEngine.Events;
using System.Threading.Tasks;


public class MasterUnlockable : NetworkBehaviour, IInteractable<PlayerBody>
{
    [SerializeField] private List<UnlockableInteractable> requiredUnlockables = new List<UnlockableInteractable>();

    public UnityEvent<bool> OnInteractionAttempt;

    public Task<bool> CanInteract(PlayerBody interactor)
    {
        return Task.FromResult(true);
    }

    [ServerRpc]
    public Task<bool> TryInteract(PlayerBody interactor)
    {
        bool isFullyUnlocked = CheckIfAllUnlocked();

        TriggerEvent_Observers(isFullyUnlocked);

        return Task.FromResult(isFullyUnlocked);
    }


    private bool CheckIfAllUnlocked()
    {
        if (requiredUnlockables.Count == 0) return true;

        foreach (UnlockableInteractable item in requiredUnlockables)
        {
            //This requires the 'public bool IsUnlocked' addition to UnlockableInteractable script
            if (!item.IsUnlocked)
            {
                return false;
            }
        }
        return true;
    }


    [ObserversRpc]
    private void TriggerEvent_Observers(bool success)
    {
        OnInteractionAttempt?.Invoke(success);

        if (success)
            Debug.Log("[ConditionalInteractable]: Interaction Successful. All requirements met.");
        else
            Debug.Log("[ConditionalInteractable]: Interaction Failed. Missing requirements.");
    }
}
