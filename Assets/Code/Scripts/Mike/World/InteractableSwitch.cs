using PurrNet;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class InteractableSwitch : NetworkBehaviour, IInteractable<MonoBehaviour>
{
    protected SyncVar<bool> isOn { get; private set; } = new(false, ownerAuth: false);
    [SerializeField] protected UnityEvent OnSwitchOn, OnSwitchOff;

    protected virtual void Awake() => isOn.onChanged += InvokeOnSwitch_ObserversRpc;
    public virtual Task<bool> CanInteract(MonoBehaviour interactor) => Task.FromResult(true);
    [ServerRpc] public Task<bool> TryInteract(MonoBehaviour interactor)
    {
        isOn.value = !isOn.value;
        return Task.FromResult(true);
    }

    [ObserversRpc] private void InvokeOnSwitch_ObserversRpc(bool isOn)
    {
        if (isOn) OnSwitchOn?.Invoke();
        else OnSwitchOff?.Invoke();
    }

    public void SetSwitch_Server(bool toggledOn)
    {
        if(!isServer) return;
        isOn.value = toggledOn;
    }
}