using System.Collections;
using System;
using UnityEngine;

public class EnvironmentLightingManager : MonoBehaviour
{
    public static EnvironmentLightingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [SerializeField] private Light directionalLight;
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private EnvironmentLightingSettings[] lightingSettings;
    private int lastAppliedIndex = 0;

    [Serializable]
    public class EnvironmentLightingSettings
    {
        public float ambientLightIntensity = 1f;
        public float ambientReflectionIntensity = 1f;
        public Color ambientShadowColor = new Color(0.42f, 0.478f, 0.627f);
        public Color directionalLightColor;
        public Material skybox;
    }

    public void SetEnvironmentLighting(int index)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateLightingChange(index));
    }

    public void ResetEnvironmentLighting()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateLightingChange(lastAppliedIndex));
    }

    private IEnumerator AnimateLightingChange(int index)
    {
        if (index >= lightingSettings.Length) yield break;
        if (index != lastAppliedIndex) lastAppliedIndex = index;

        float elapsedTime = 0f;
        // Store initial values
        float initialAmbientIntensity = RenderSettings.ambientIntensity;
        float initialReflectionIntensity = RenderSettings.reflectionIntensity;
        Color initialAmbientLight = RenderSettings.ambientLight;
        Color initialDirectionalLightColor = Color.black;
        if (directionalLight != null) initialDirectionalLightColor = directionalLight.color;
        // Target values from the selected settings
        EnvironmentLightingSettings targetSettings = lightingSettings[index];
        float targetAmbientIntensity = targetSettings.ambientLightIntensity;
        float targetReflectionIntensity = targetSettings.ambientReflectionIntensity;
        Color targetAmbientLight = targetSettings.ambientShadowColor;
        Color targetDirectionalLightColor = Color.black;
        if (directionalLight != null) targetDirectionalLightColor = targetSettings.directionalLightColor;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            // Lerp between initial and target values
            RenderSettings.ambientIntensity = Mathf.Lerp(initialAmbientIntensity, targetAmbientIntensity, t);
            RenderSettings.reflectionIntensity = Mathf.Lerp(initialReflectionIntensity, targetReflectionIntensity, t);
            RenderSettings.ambientLight = Color.Lerp(initialAmbientLight, targetAmbientLight, t);
            if (directionalLight != null) directionalLight.color = Color.Lerp(initialDirectionalLightColor, targetDirectionalLightColor, t);
            yield return null; // Wait for the next frame
        }
        // Ensure final values are set
        RenderSettings.ambientIntensity = targetAmbientIntensity;
        RenderSettings.reflectionIntensity = targetReflectionIntensity;
        RenderSettings.ambientLight = targetAmbientLight;
        if (directionalLight != null) directionalLight.color = targetDirectionalLightColor;
        if (targetSettings.skybox != null) RenderSettings.skybox = targetSettings.skybox;
    }
}
