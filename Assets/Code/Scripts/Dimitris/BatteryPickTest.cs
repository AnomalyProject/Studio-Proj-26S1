using UnityEngine;
/// <summary>
/// A tester that sets an energyamount on an object and if player enters on collider adds directly durability
/// </summary>
public class BatteryPickTest : MonoBehaviour
{
    [SerializeField] private float energyAmount = 40f;
    private void OnTriggerEnter(Collider other)
    {
        Flashlight flashlight = other.GetComponent<Flashlight>();
        if (flashlight != null)
        {
            flashlight.RechargeDurability(energyAmount);
            Destroy(gameObject);
        }
    }
}
