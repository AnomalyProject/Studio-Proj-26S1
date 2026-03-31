using UnityEngine;

public class ExampleInteractable : MonoBehaviour, IInteractable<MonoBehaviour>
{
    public bool CanInteract(MonoBehaviour interactor) => true;

    public bool TryInteract(MonoBehaviour interactor)
    {
        Debug.Log("Interacted With:" + gameObject.name);
        return true;
    }
}
