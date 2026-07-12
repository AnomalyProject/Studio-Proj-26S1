using System.Collections;
using UnityEngine;

public class LightSparkTrigger : MonoBehaviour
{
    [Header("Light Settings")]
    [Tooltip("Drag the Light components you want to spark here. You can expand the list to add more!")]
    public Light[] targetLights; 
    
    [Tooltip("How many times the lights should spark.")]
    public int numberOfSparks = 3;
    
    [Tooltip("How fast the spark flashes (in seconds).")]
    public float sparkSpeed = 0.1f;

    [Header("Trigger Settings")]
    [Tooltip("If checked, the effect only happens the very first time the player walks through.")]
    public bool triggerOnlyOnce = false;

    [Tooltip("How many seconds to wait before it can trigger again.")]
    public float cooldownTime = 2.0f;

    private bool hasTriggeredOnce = false;
    private bool isOnCooldown = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggeredOnce) return;
            if (isOnCooldown) return;
            if (targetLights.Length > 0)
            {
                hasTriggeredOnce = true;
                StartCoroutine(SparkRoutine());
            }
            else
            {
                Debug.LogWarning("Cannot spark: No Light components were assigned in the Inspector!");
            }
        }
    }

    private IEnumerator SparkRoutine()
    {
        isOnCooldown = true;
        
        bool[] originalStates = new bool[targetLights.Length];
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
            {
                originalStates[i] = targetLights[i].enabled;
            }
        }
        
        for (int i = 0; i < numberOfSparks; i++)
        {
            for (int j = 0; j < targetLights.Length; j++)
            {
                if (targetLights[j] != null)
                {
                    targetLights[j].enabled = !originalStates[j];
                }
            }
            yield return new WaitForSeconds(sparkSpeed);
            
            for (int j = 0; j < targetLights.Length; j++)
            {
                if (targetLights[j] != null)
                {
                    targetLights[j].enabled = originalStates[j];
                }
            }
            yield return new WaitForSeconds(sparkSpeed);
        }
        
        yield return new WaitForSeconds(cooldownTime);
        
        isOnCooldown = false;
    }
}