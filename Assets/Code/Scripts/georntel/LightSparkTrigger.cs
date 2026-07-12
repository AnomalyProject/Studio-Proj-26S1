using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeonSparkTrigger : MonoBehaviour
{
    [Header("Neon Settings")]
    [Tooltip("Drag your scene objects here. The script will automatically find all the glowing parts inside!")]
    public GameObject[] neonObjects; 

    [Tooltip("Drag the actual Unity Light components (Point, Spot, etc.) here to make them flash in sync.")]
    public Light[] neonLights;

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
    
    // Material caching
    private List<Renderer> glowingRenderers = new List<Renderer>();
    private List<Color> originalEmissionColors = new List<Color>();
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // Light caching
    private float[] originalLightIntensities;

    private void Start()
    {
        propBlock = new MaterialPropertyBlock();

        // 1. Setup Material Data
        foreach (GameObject obj in neonObjects)
        {
            if (obj != null)
            {
                Renderer[] renderersInChildren = obj.GetComponentsInChildren<Renderer>();
                
                foreach (Renderer rend in renderersInChildren)
                {
                    if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(EmissionColorID))
                    {
                        glowingRenderers.Add(rend);
                        originalEmissionColors.Add(rend.sharedMaterial.GetColor(EmissionColorID));
                    }
                }
            }
        }

        // 2. Setup Light Data
        originalLightIntensities = new float[neonLights.Length];
        for (int i = 0; i < neonLights.Length; i++)
        {
            if (neonLights[i] != null)
            {
                originalLightIntensities[i] = neonLights[i].intensity;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggeredOnce) return;
            if (isOnCooldown) return;
            
            if (glowingRenderers.Count > 0 || neonLights.Length > 0)
            {
                hasTriggeredOnce = true;
                StartCoroutine(SparkRoutine());
            }
            else
            {
                Debug.LogWarning("Cannot spark: No glowing parts or lights were assigned!");
            }
        }
    }

    private IEnumerator SparkRoutine()
    {
        isOnCooldown = true;
        
        for (int i = 0; i < numberOfSparks; i++)
        {
            // TURN OFF (Material + Lights)
            SetEmissionColor(Color.black);
            SetLightIntensity(0f);
            yield return new WaitForSeconds(sparkSpeed);
            
            // TURN ON (Material + Lights)
            RestoreOriginalEmission();
            RestoreOriginalLights();
            yield return new WaitForSeconds(sparkSpeed);
        }
        
        yield return new WaitForSeconds(cooldownTime);
        
        isOnCooldown = false;
    }

    // --- MATERIAL HELPERS ---
    private void SetEmissionColor(Color color)
    {
        for (int j = 0; j < glowingRenderers.Count; j++)
        {
            if (glowingRenderers[j] != null)
            {
                glowingRenderers[j].GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorID, color);
                glowingRenderers[j].SetPropertyBlock(propBlock);
            }
        }
    }

    private void RestoreOriginalEmission()
    {
        for (int j = 0; j < glowingRenderers.Count; j++)
        {
            if (glowingRenderers[j] != null)
            {
                glowingRenderers[j].GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorID, originalEmissionColors[j]);
                glowingRenderers[j].SetPropertyBlock(propBlock);
            }
        }
    }

    // --- LIGHT HELPERS ---
    private void SetLightIntensity(float intensity)
    {
        for (int i = 0; i < neonLights.Length; i++)
        {
            if (neonLights[i] != null)
            {
                neonLights[i].intensity = intensity;
            }
        }
    }

    private void RestoreOriginalLights()
    {
        for (int i = 0; i < neonLights.Length; i++)
        {
            if (neonLights[i] != null)
            {
                neonLights[i].intensity = originalLightIntensities[i];
            }
        }
    }
}