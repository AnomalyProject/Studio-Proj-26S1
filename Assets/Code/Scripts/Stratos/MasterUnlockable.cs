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

    [ServerRpc]                                               //ensures that the server handles the logic check
    public Task<bool> TryInteract(PlayerBody interactor)
    {
        bool isFullyUnlocked = CheckIfAllUnlocked();         //checks if player has all requirments

        TriggerEvent_Observers(isFullyUnlocked);

        return Task.FromResult(isFullyUnlocked);
    }

    //method that loops through the checklist and verify if every lock is open
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


    [ObserversRpc]                                              //the information goes to all players in the game
    private void TriggerEvent_Observers(bool success)
    {
        OnInteractionAttempt?.Invoke(success);

        if (success)
            Debug.Log("[ConditionalInteractable]: Interaction Successful. All requirements met.");
        else
            Debug.Log("[ConditionalInteractable]: Interaction Failed. Missing requirements.");
    }
}
