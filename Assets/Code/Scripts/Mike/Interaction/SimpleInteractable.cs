using PurrNet;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class SimpleInteractable : NetworkBehaviour, IInteractable<MonoBehaviour>
{
    [SerializeField] private UnityEvent OnInteracted;
    public Task<bool> CanInteract(MonoBehaviour interactor) => Task.FromResult(true);

    [ServerRpc] public Task<bool> TryInteract(MonoBehaviour interactor)
    {
        InvokeInteracted();
        return Task.FromResult(true);
    }

    [ObserversRpc] private void InvokeInteracted() => OnInteracted?.Invoke();
}
