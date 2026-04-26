using System.Threading.Tasks;
using UnityEngine;
using PurrNet;

public class KeypadButton : NetworkBehaviour, IInteractable<MonoBehaviour>
{
    [SerializeField, Range(-1, 9)] private int myDigit = 1; // the value this current button will type into the keypad, limited to 0 - 9 values
                                                            // -1 = clear


    public KeypadInteractable myKeypad; // reference to the keypad script, should be automatically assigned by the keypad, can be assigned manually in case it fails


    public Task<bool> CanInteract(MonoBehaviour interactor)
    {
        // Always interactable as long as a Keypad is assigned.
        return Task.FromResult(myKeypad != null);
    }

    public Task<bool> TryInteract(MonoBehaviour interactor)
    {
        myKeypad.PressDigit(myDigit);
        return Task.FromResult(true);
    }

    void OnMouseDown()  // Debug
    {
        if (myKeypad != null && myKeypad.debugMouseInput)
        {
            myKeypad.PressDigit(myDigit);
        }
    }
}
