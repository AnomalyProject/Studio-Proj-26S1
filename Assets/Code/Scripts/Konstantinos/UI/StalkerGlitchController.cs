using UnityEngine;
using PurrNet;
using System;

public class StalkerGlitchController : MonoBehaviour
{
    // Events
    public event Action<PlayerBody> onStartedLookingAtStalker;
    public event Action<PlayerBody> onStoppedLookingAtStalker;
    public event Action<PlayerBody> onGlitchMaxed;
    public event Action<PlayerBody> onGlitchZero;

    [Header("References")]
    [SerializeField] private PlayerBody playerBody;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Material glitchMaterial;
    [SerializeField] private NetworkIdentity identity;

    [Header("Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstructionLayers; // anything other than the stalker meant to block visibility

    [Header("Glitch Timing")]
    [SerializeField] private float glitchInSpeed = 3f;
    [SerializeField] private float glitchOutSpeed = 1f; 

    [Header("Shader Values")]
    [SerializeField] private float maxNoiseAmount = 1f;
    [SerializeField] private float maxGlitchStrength = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float minScanLinesTransparency = 0.2f;

    [Header("Search")]
    [SerializeField] private float visibilitySearchRadius = 1000f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource glitchAudioSource;
    [SerializeField] private AudioClip glitchLoopClip;
    [SerializeField] private float maxGlitchVolume = 1f;
    [SerializeField] private bool playLoopAutomatically = true;

    private float currentIntensity;

    private bool wasLookingAtStalker;
    private bool glitchWasMaxed;
    private bool glitchWasZero = true;

    private readonly Plane[] frustumPlanes = new Plane[6];


    private void Awake()
    {
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        if (glitchAudioSource == null)
            return;

        if (glitchLoopClip != null)
        {
            glitchAudioSource.clip = glitchLoopClip;
        }

        glitchAudioSource.loop = true;
        glitchAudioSource.volume = 0f;

        if (playLoopAutomatically &&
            glitchAudioSource.clip != null &&
            !glitchAudioSource.isPlaying)
        {
            glitchAudioSource.Play();
        }
    }

    private void Update()
    {
        if (!identity.isOwner) return; // only local player can change the glitch shader

        bool lookingAtStalker = IsAnyEnemyVisible();

        HandleVisibilityEvents(lookingAtStalker);

        float targetIntensity = lookingAtStalker ? 1f : 0f;
        float speed = lookingAtStalker
            ? glitchInSpeed
            : glitchOutSpeed;

        currentIntensity = Mathf.MoveTowards(
            currentIntensity,
            targetIntensity,
            speed * Time.deltaTime);

        UpdateShader();
        HandleIntensityEvents();
        UpdateAudio();
    }

    private void HandleVisibilityEvents(bool lookingAtStalker)
    {
        if (lookingAtStalker && !wasLookingAtStalker)
        {
            onStartedLookingAtStalker?.Invoke(playerBody);
        }

        if (!lookingAtStalker && wasLookingAtStalker)
        {
            onStoppedLookingAtStalker?.Invoke(playerBody);
        }

        wasLookingAtStalker = lookingAtStalker;
    }

    private void HandleIntensityEvents()
    {
        const float epsilon = 0.001f;

        bool isMaxed = currentIntensity >= 1f - epsilon;
        bool isZero = currentIntensity <= epsilon;

        if (isMaxed && !glitchWasMaxed)
        {
            onGlitchMaxed?.Invoke(playerBody);
        }

        if (isZero && !glitchWasZero)
        {
            onGlitchZero?.Invoke(playerBody);
        }

        glitchWasMaxed = isMaxed;
        glitchWasZero = isZero;
    }

    private bool IsAnyEnemyVisible()
    {
        GeometryUtility.CalculateFrustumPlanes(
            playerCamera,
            frustumPlanes);

        Collider[] enemies = Physics.OverlapSphere(
            playerCamera.transform.position,
            visibilitySearchRadius,
            enemyLayer,
            QueryTriggerInteraction.Ignore);

        foreach (Collider enemy in enemies)
        {
            Bounds bounds = enemy.bounds;

            bool insideFrustum =
                GeometryUtility.TestPlanesAABB(
                    frustumPlanes,
                    bounds);

            if (!insideFrustum)
                continue;

            if (HasLineOfSight(enemy))
                return true;
        }

        return false;
    }

    private bool HasLineOfSight(Collider enemy)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 target = enemy.bounds.center;

        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(
                origin,
                direction.normalized,
                out RaycastHit hit,
                distance,
                obstructionLayers | enemyLayer,
                QueryTriggerInteraction.Ignore))
        {
            return ((1 << hit.collider.gameObject.layer) & enemyLayer) != 0;
        }

        return false;
    }

    private void UpdateShader()
    {
        glitchMaterial.SetFloat(
            "_NoiseAmount",
            Mathf.Lerp(
                0f,
                maxNoiseAmount,
                currentIntensity));

        glitchMaterial.SetFloat(
            "_GlitchStrength",
            Mathf.Lerp(
                0f,
                maxGlitchStrength,
                currentIntensity));

        glitchMaterial.SetFloat(
            "_ScanLinesTransparency",
            Mathf.Lerp(
                1f,
                minScanLinesTransparency,
                currentIntensity));
    }

    private void UpdateAudio()
    {
        if (glitchAudioSource == null)
            return;

        glitchAudioSource.volume =
            currentIntensity * maxGlitchVolume;
    }

    public float CurrentIntensity => currentIntensity;

    public bool IsLookingAtStalker => wasLookingAtStalker;
}