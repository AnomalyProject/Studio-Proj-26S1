using UnityEngine;

public class ExampleInteractable : MonoBehaviour, IInteractable<FPSController>
{
    public bool CanInteract(FPSController interactor) => true;

    public bool TryInteract(FPSController interactor)
    {
        Debug.Log("Interacted With:" + gameObject.name);
        return true;
    }
}
