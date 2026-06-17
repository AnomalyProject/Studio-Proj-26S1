using static AnomalyManager;
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
    [SerializeField] private EnvironmentLightingSettings winRoomLightingSettings;
    [SerializeField] private EnvironmentLightingSettings[] voidLightingSettings;
    private EnvironmentLightingSettings lastAppliedSettings;
    private EnvironmentLightingSettings appliedSettings;

    [Serializable]
    public class EnvironmentLightingSettings
    {
        public float ambientLightIntensity = 1f;
        public float ambientReflectionIntensity = 1f;
        public Color ambientShadowColor = new Color(0.42f, 0.478f, 0.627f);
        public Color directionalLightColor;
        public Material skybox;
    }

    public void SetEnvironmentLighting(MapStateData data)
    {
        EnvironmentLightingSettings targetSettings = data.roomState switch
        {
            RoomState.PunishmentRoom => voidLightingSettings[data.uniqueRoomIndex],
            RoomState.WinRoom => winRoomLightingSettings,
            _ => lightingSettings[data.mapIndex]
        };
        if (targetSettings != appliedSettings)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateLightingChange(targetSettings));
        }
    }
    public void SetEnvironmentLighting(int index)
    {
        EnvironmentLightingSettings targetSettings = lightingSettings[index];
        if (targetSettings != appliedSettings)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateLightingChange(targetSettings));
        }
    }
    
    public void ResetEnvironmentLighting()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateLightingChange(lastAppliedSettings));
    }

    private IEnumerator AnimateLightingChange(EnvironmentLightingSettings settings)
    {
        float animationDuration = this.animationDuration;
        if (Time.timeSinceLevelLoad < 0.1f)
        {
            animationDuration = 0.1f;
            // Wait for RenderSettings to initialize
            yield return new WaitForEndOfFrame();
        }

        // Target values from the selected settings
        if (settings == appliedSettings) yield break;
        appliedSettings = settings;
        if (lastAppliedSettings != settings) lastAppliedSettings = settings;

        float elapsedTime = 0f;
        // Store initial values
        float initialAmbientIntensity = RenderSettings.ambientIntensity;
        float initialReflectionIntensity = RenderSettings.reflectionIntensity;
        Color initialAmbientLight = RenderSettings.ambientLight;
        Color initialDirectionalLightColor = Color.black;
        if (directionalLight != null) initialDirectionalLightColor = directionalLight.color;
        float targetAmbientIntensity = settings.ambientLightIntensity;
        float targetReflectionIntensity = settings.ambientReflectionIntensity;
        Color targetAmbientLight = settings.ambientShadowColor;
        Color targetDirectionalLightColor = Color.black;
        if (directionalLight != null) targetDirectionalLightColor = settings.directionalLightColor;
        if (settings.skybox != null) RenderSettings.skybox = settings.skybox;
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
    }
}
