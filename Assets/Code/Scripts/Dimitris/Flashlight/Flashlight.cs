using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.Events;
/// <summary>
/// Flashlight class that implements an interaction interface , basically switch lights on and off
/// There is a capacity of energy that keeps on the light and when its durability fails turns off 
/// and cannot open untill energy comes back 
/// </summary>
public class Flashlight : MonoBehaviour, IInteractable<MonoBehaviour>
{
    [SerializeField] Light flashlightLight;
    [SerializeField, Range(30, 600)] private float maxDurabilitySeconds = 10f;
    [SerializeField, Range(.1f, 5f)] float drainSpeedMultiplier = 1f;
    [SerializeField] private bool canBeUsed = false;
    [SerializeField] private bool drainsBattery;

    float durability = 0f;
    float minDrainSpeedMult = .1f;
    private bool flashlightOn = false;
    public UnityEvent OnToggleOn, OnToggleOff, OnDrained;
    public float NormalizedDurability => durability / maxDurabilitySeconds;

    public Task<bool> CanInteract(MonoBehaviour Interactor)
    {
        Debug.Log("canBeUsed: " + canBeUsed + " durability: " + durability);
        bool result = canBeUsed && (!drainsBattery || durability > 0f); //If it has no durability or cant be used doesnt allow to interact
        return Task.FromResult(result); 
    }
    //Atemps to interact with flashlight
    public Task<bool> TryInteract(MonoBehaviour Interactor)
    {
        ToggleFlashlight();
        Debug.Log("Flashlight Interacted with " + Interactor.name);
        return Task.FromResult(true);
    }   
    private void Start()
    {
        durability = maxDurabilitySeconds;
        flashlightLight.enabled = false;
    }
    private void Update()
    {
        if (flashlightOn && drainsBattery)
        {
            DurabilityDrop(Time.deltaTime);
        }
    }

    //Switch States if requirements for toggle are true opens the light else closes it
    private void ToggleFlashlight()
    {
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
        Debug.Log("TURN ON");
        if (drainsBattery && durability <= 0f) return;
        flashlightOn = true;
        flashlightLight.enabled = true;
        OnToggleOn?.Invoke();
    }

    //Off light
    private void ToggleFlashlightOff()
    {
        Debug.Log("TURN Off");
        flashlightOn = false;
        flashlightLight.enabled = false;
        OnToggleOff?.Invoke();
    }
    //Drops Battery by dropBattery time and if reach 0 closes light
    private void DurabilityDrop(float deltaTime)
    {
        if (!drainsBattery) return;

        durability -= deltaTime * drainSpeedMultiplier;

        if (durability <= 0f)
        {
            durability = 0f;
            ToggleFlashlightOff();
            OnDrained?.Invoke();
        }
    }

    public void ChangeDrainSpeed(float speedMultiplier) => drainSpeedMultiplier = Mathf.Max(speedMultiplier, minDrainSpeedMult);
    public void SetDrainsBattery(bool drainsBattery) => this.drainsBattery = drainsBattery;
    public void SetCanBeUsed(bool canBeUsed) => this.canBeUsed = canBeUsed;

    //Fully Recharges on call
    public void FullRecharge()
    {
        durability = maxDurabilitySeconds;
    }
    //Recharge by adding from something 
    public void AffectDurability(float amountEnergy)
    {
        //Ensures that the new value will never be less than 0 nor greater than maxDurability
        durability = Math.Clamp(durability + amountEnergy, 0f, maxDurabilitySeconds);
    }
}