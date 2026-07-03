using System.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine;
using PurrNet;

public class SimpleInteractable : NetworkBehaviour, IInteractable<MonoBehaviour>
{
    public UnityEvent OnInteracted;
    public Task<bool> CanInteract(MonoBehaviour interactor) => Task.FromResult(true);

    [ServerRpc]
    public Task<bool> TryInteract(MonoBehaviour interactor)
    {
        InvokeInteracted();
        return Task.FromResult(true);
    }

    [ObserversRpc]
    private void InvokeInteracted() => OnInteracted?.Invoke();
}
