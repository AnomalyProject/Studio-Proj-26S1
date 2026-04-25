using System.Threading.Tasks;
using UnityEngine;

public class ExampleInteractable : MonoBehaviour, IInteractable<MonoBehaviour>
{
    public Task<bool> CanInteract(MonoBehaviour interactor) => Task.FromResult(true);

    public Task<bool> TryInteract(MonoBehaviour interactor)
    {
        Debug.Log("Interacted With:" + gameObject.name);
        return Task.FromResult(true);
    }
}
