using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeonSparkTrigger : MonoBehaviour
{
    [Header("Neon Settings")]
    [Tooltip("Drag your entire Neon Prefab(s) here. The script will automatically find all the glowing parts inside!")]
    public GameObject[] neonPrefabs;

    [Tooltip("How many times the neon should spark.")]
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
    private List<Renderer> glowingRenderers = new List<Renderer>();
    private List<Color> originalEmissionColors = new List<Color>();

    private void Start()
    {
        foreach (GameObject prefab in neonPrefabs)
        {
            if (prefab != null)
            {
                Renderer[] renderersInChildren = prefab.GetComponentsInChildren<Renderer>();
                
                foreach (Renderer rend in renderersInChildren)
                {
                    if (rend.material.HasProperty("_EmissionColor"))
                    {
                        glowingRenderers.Add(rend);
                        originalEmissionColors.Add(rend.material.GetColor("_EmissionColor"));
                    }
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggeredOnce) return;
            if (isOnCooldown) return;
            
            if (glowingRenderers.Count > 0)
            {
                hasTriggeredOnce = true;
                StartCoroutine(SparkRoutine());
            }
            else
            {
                Debug.LogWarning("Cannot spark: No glowing neon parts were found in the assigned prefabs!");
            }
        }
    }

    private IEnumerator SparkRoutine()
    {
        isOnCooldown = true;
        
        for (int i = 0; i < numberOfSparks; i++)
        {
            //  TURN EMISSION OFF 
            for (int j = 0; j < glowingRenderers.Count; j++)
            {
                if (glowingRenderers[j] != null)
                {
                    glowingRenderers[j].material.SetColor("_EmissionColor", Color.black);
                }
            }
            
            yield return new WaitForSeconds(sparkSpeed);
            
            //  TURN EMISSION ON 
            for (int j = 0; j < glowingRenderers.Count; j++)
            {
                if (glowingRenderers[j] != null)
                {
                    glowingRenderers[j].material.SetColor("_EmissionColor", originalEmissionColors[j]);
                }
            }
            
            yield return new WaitForSeconds(sparkSpeed);
        }
        
        yield return new WaitForSeconds(cooldownTime);
        
        isOnCooldown = false;
    }
}