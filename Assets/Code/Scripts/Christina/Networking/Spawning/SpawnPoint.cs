using UnityEngine;

/// <summary>
/// Component that marks a GameObject as a player spawn location.
/// Place on empty GameObjects in the scene. SpawnManager finds these at runtime.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    // added HideInInspector because we do want the float public but we don't want it showing in the insprector
    // there is no need for that. 
    [HideInInspector] public float LastUsedTime = float.MinValue;
    
    // I chose OnDrawGizmos and not OnDrawGizmosSelected because the first keeps tehe gizmos
    // drawn in the scene constantly. 
    private void OnDrawGizmos()
    {
        // sphere to show position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // ray to show direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}
