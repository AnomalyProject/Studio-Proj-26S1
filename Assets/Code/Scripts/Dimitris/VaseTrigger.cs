using UnityEngine;

public class VaseTrigger : MonoBehaviour
{
    [Header("Vase")]
    public Renderer vaseRenderer;
    public Color emissionColor = Color.magenta;
    public float emissionIntensity = 0.25f;

    [Header("Music")]
    public AudioSource backgroundMusic;
    public float triggeredPitch = 2f;

    [Header("Settings")]
    public string playerTag = "Player";

    private Material mat;
    private Color originalEmission;
    private float originalPitch;

    private void Start()
    {
        // Cache the material instance and store the original values
        mat = vaseRenderer.material;
        originalEmission = mat.GetColor("_EmissionColor");

        if (backgroundMusic != null)
            originalPitch = backgroundMusic.pitch;
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (!other.CompareTag(playerTag))
            return;

        // Enable the vase emission effect
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);

        // Speed up the background music while the player is nearby
        if (backgroundMusic != null)
            backgroundMusic.pitch = triggeredPitch;
    }

    private void OnTriggerExit(Collider other)
    {
        
        if (!other.CompareTag(playerTag))
            return;

        // Restore the original vase appearance
        mat.SetColor("_EmissionColor", originalEmission);

        // Restore the original music speed
        if (backgroundMusic != null)
            backgroundMusic.pitch = originalPitch;
    }
}