using UnityEngine;
using System;
/// <summary>
/// Flashlight class that implements an interaction interface , basically switch lights on and off
/// There is a capacity of energy that keeps on the light and when its durability fails turns off 
/// and cannot open untill energy comes back 
/// </summary>
public class Flashlight : MonoBehaviour, IInteractable<MonoBehaviour>
{
    [SerializeField] Light flashlightLigth;
    [SerializeField] float dropBatteryTime = 1f; //The rythm that drops the battery
    [SerializeField] float rechargeBatteryTime = 1f; //The rythm that recharges the battery,when step on field
    [SerializeField] private float maxDurability =10f;
    [SerializeField] private bool canBeUsed = false;
    public float durability = 0f;
    private bool flashlightOn = false;
    public static event Action OnToggleOn , OnToggleOff;
    public float NormalizedDurability => durability / maxDurability;

    public bool CanInteract(MonoBehaviour Interactor)
    {
        Debug.Log("canBeUsed: " + canBeUsed + " durability: " + durability);
        return canBeUsed && durability > 0f;  //If it has no durability or cant be used doesnt allow to interact
    }
    //Atemps to interact with flashlight
    public bool TryInteract(MonoBehaviour Interactor)
    {
        if(!CanInteract(Interactor))
        {
            return false;
        }
        ToggleFlashlight();
        Debug.Log("Flashlight Interacted with " + Interactor.name);
        return true;
    }
    private void Start()
    {
        durability = maxDurability;
        flashlightLigth.enabled = false;
    }
    private void Update()
    {
        //If lights on starts drop battery untill close
        if (flashlightOn)
        {
            Debug.Log("DRAINING");
            DurabilityDrop();
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
        if (durability <= 0f) return;
        flashlightOn = true;
        flashlightLigth.enabled = true;
        OnToggleOn?.Invoke();
    }
    //Off light
    private void ToggleFlashlightOff()
    {
        Debug.Log("TURN Off");
        flashlightOn = false;
        flashlightLigth.enabled = false;
        OnToggleOff?.Invoke();
    }
    //Drops Battery by dropBattery time and if reach 0 closes light
    private void DurabilityDrop()
    {
        durability -= Time.deltaTime /dropBatteryTime;
            if (durability <= 0f)
            {
                durability = 0f;
                ToggleFlashlightOff();
               
            }
        
    }
    //Fully Recharges on call
    public void FullRecharge()
    {
        durability = maxDurability;
    }
    //Recharge by adding from something 
    public void RechargeDurability(float amountEnergy)
    {
        //Ånsures that the new value will never be less than 0 nor greater than maxDurability
        durability = Math.Clamp(durability + amountEnergy, 0f, maxDurability);
    }
    //Recharges as long as stays inside collider
    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Recharge Space"))
        {
            RechargeDurability(6*Time.deltaTime/rechargeBatteryTime);
        }
    }
}
