using System.Collections;
using UnityEngine;

public class BlindingLightTrigger : MonoBehaviour
{
    [Header("Light Settings")]
    [Tooltip("Drag the Light components you want to flash here.")]
    public Light[] targetLights;
    
    [Tooltip("How bright the lights get when they blind the player.")]
    public float blindingIntensity = 100f; 

    [Header("Timing Settings")]
    [Tooltip("How long it takes to smoothly reach maximum brightness (in seconds).")]
    public float fadeUpTime = 0.2f;

    [Tooltip("How long the player stays fully blinded at maximum brightness (in seconds).")]
    public float blindDuration = 0.5f;

    [Tooltip("How long it takes for the light to smoothly fade back to normal (in seconds).")]
    public float fadeDownTime = 1.0f;

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
                StartCoroutine(SmoothBlindRoutine());
            }
            else
            {
                Debug.LogWarning("Cannot flash: No Light components were assigned in the Inspector!");
            }
        }
    }

    private IEnumerator SmoothBlindRoutine()
    {
        isOnCooldown = true;
        
        float[] originalIntensities = new float[targetLights.Length];
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
            {
                originalIntensities[i] = targetLights[i].intensity;
            }
        }
        
        float elapsed = 0f;
        while (elapsed < fadeUpTime)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / fadeUpTime;
            
            for (int i = 0; i < targetLights.Length; i++)
            {
                if (targetLights[i] != null)
                {
                    targetLights[i].intensity = Mathf.Lerp(originalIntensities[i], blindingIntensity, percent);
                }
            }
            yield return null; 
        }
        
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null) targetLights[i].intensity = blindingIntensity;
        }
        
        yield return new WaitForSeconds(blindDuration);
        
        elapsed = 0f;
        while (elapsed < fadeDownTime)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / fadeDownTime;
            
            for (int i = 0; i < targetLights.Length; i++)
            {
                if (targetLights[i] != null)
                {
                    targetLights[i].intensity = Mathf.Lerp(blindingIntensity, originalIntensities[i], percent);
                }
            }
            yield return null; 
        }
        
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null) targetLights[i].intensity = originalIntensities[i];
        }
        
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }
}