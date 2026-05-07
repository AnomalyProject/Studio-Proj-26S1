using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.Events;
using PurrNet;
/// <summary>
/// Flashlight class that implements an interaction interface , basically switch lights on and off
/// There is a capacity of energy that keeps on the light and when its durability fails turns off 
/// and cannot open untill energy comes back 
/// </summary>
public class Flashlight : NetworkBehaviour, IInteractable<MonoBehaviour>
{
    [SerializeField] Light flashlightLight;
    [SerializeField, Range(30, 600)] private float maxDurabilitySeconds = 10f;
    [SerializeField, Range(.1f, 5f)] float drainSpeedMultiplier = 1f;
    [SerializeField] private bool canBeUsed = false;
    [SerializeField] private bool drainsBattery;

    SyncVar<float> durability = new(0f, ownerAuth: false);
    float minDrainSpeedMult = .1f;
    private bool flashlightOn = false;
    public UnityEvent OnToggleOn, OnToggleOff, OnDrained;
    public float NormalizedDurability => durability / maxDurabilitySeconds;

    private Coroutine drainRoutine;
    // Initializes flashlight state when the network object spawns

    private void Awake()
    {
        flashlightLight.enabled = false;
        flashlightOn = false;
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer) durability.value = maxDurabilitySeconds;
    }

    protected override void OnObserverAdded(PlayerID player)
    {
        base.OnObserverAdded(player);
        if (isServer) flashlightTargetRpc(player, flashlightLight.enabled);
    }

    public Task<bool> CanInteract(MonoBehaviour Interactor)
    {
        Debug.Log("canBeUsed: " + canBeUsed + " durability: " + durability.value);
        bool result = canBeUsed && (!drainsBattery || durability.value > 0f); //If it has no durability or cant be used doesnt allow to interact
        return Task.FromResult(result);
    }
    //Atemps to interact with flashlight
    [ServerRpc]
    public Task<bool> TryInteract(MonoBehaviour Interactor)
    {
        ToggleFlashlight();
        Debug.Log("Flashlight Interacted with " + Interactor.name);
        return Task.FromResult(true);
    }


    //Switch States if requirements for toggle are true opens the light else closes it
    private void ToggleFlashlight()
    {
        if (!isServer || !canBeUsed) return;
        if (!flashlightOn)
        {
            ToggleFlashlightOn();
        }
        else
        {
            ToggleFlashlightOff();
        }
    }

    //On light
    private void ToggleFlashlightOn()
    {
        if (!isServer) return;
        if (drainsBattery && durability.value <= 0f) return;

        flashlightObserversState(true);

        if (drainsBattery && drainRoutine == null)
        {
            drainRoutine = StartCoroutine(DrainRoutine());
        }
    }

    //Off light
    private void ToggleFlashlightOff()
    {
        if (!isServer) return;
        flashlightObserversState(false);
        if (drainRoutine != null)
        {
            StopCoroutine(drainRoutine);
            drainRoutine = null;
        }
    }

    // Called on all clients to sync the flashlight's On/Off state and triggers events
    [ObserversRpc] private void flashlightObserversState(bool state) => SetFlashlight(state);
    [TargetRpc] private void flashlightTargetRpc(PlayerID player, bool state) => SetFlashlight(state);
    private void SetFlashlight(bool state)
    {
        if (flashlightOn == state) return;

        flashlightOn = state;
        flashlightLight.enabled = state;

        if (state)
        {
            OnToggleOn?.Invoke();
        }
        else
        {
            OnToggleOff?.Invoke();
        }
    }

    // Drains battery in fixed time intervals (instead of every frame) while the flashlight is on.
    private IEnumerator DrainRoutine()
    {
        if (!isServer) yield break;

        WaitForSeconds wait = new WaitForSeconds(0.25f);

        while (flashlightOn && drainsBattery)
        {
            yield return wait;
            durability.value -= 0.25f * drainSpeedMultiplier;

            if (durability <= 0f)
            {
                durability.value = 0f;
                flashlightObserversState(false);
                OnDrained?.Invoke();
                drainRoutine = null;
                yield break;
            }
        }
        drainRoutine = null;
    }

    public void ChangeDrainSpeed(float speedMultiplier)
    {
        if (!isServer) return;
        drainSpeedMultiplier = Mathf.Max(speedMultiplier, minDrainSpeedMult);
    }
    public void SetDrainsBattery(bool drainsBattery)
    {
        if (!isServer) return;
        this.drainsBattery = drainsBattery;
    }
    public void SetCanBeUsed(bool canBeUsed)
    {
        if (!isServer) return;
        this.canBeUsed = canBeUsed;
    }

    //Fully Recharges on call
    public void FullRecharge()
    {
        if (!isServer) return;
        durability.value = maxDurabilitySeconds;
    }
    //Recharge by adding from something 
    public void AffectDurability(float amountEnergy)
    {
        if (!isServer) return;
        //Ensures that the new value will never be less than 0 nor greater than maxDurability
        durability.value = Math.Clamp(durability.value + amountEnergy, 0f, maxDurabilitySeconds);
    }
}