using UnityEngine;
using System.Threading.Tasks;

public class LobbyLevelSelectionButton : MonoBehaviour, IInteractable<PlayerBody>
{
    private enum ButtonType
    {
        Previous,
        Next
    }
    
    [SerializeField] private LobbyLevelSelectionTV tv;
    [SerializeField] private ButtonType buttonType;
   
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        return Task.FromResult(tv != null && tv.CanLocalPlayerControl());
    }

    public Task<bool> TryInteract(PlayerBody interactor)
    {
        if (tv == null) return Task.FromResult(false);

        switch (buttonType)
        {
            case ButtonType.Previous:
                tv.RequestPreviousLevel();
                break;

            case ButtonType.Next:
                tv.RequestNextLevel();
                break;
        }

        return Task.FromResult(true);
    }
}
