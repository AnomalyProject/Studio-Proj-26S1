using System.Threading.Tasks;
using UnityEngine;

public class ChalkCanister : PlayerItem, IInteractable<PlayerBody>
{
    
    
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> TryInteract(PlayerBody interactor)
    {
        throw new System.NotImplementedException();
    }
}
