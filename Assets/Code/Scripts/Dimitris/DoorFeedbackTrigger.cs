using UnityEngine;

public class DoorFeedbackTrigger : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip doorSound;

    [Header("Visual Effect")]
    public GameObject visualEffectPrefab;

    [Header("Settings")]
    public string playerTag = "Player";
    public float cooldown = 6f;

    private float lastTriggerTime;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (Time.time < lastTriggerTime + cooldown) return;

        lastTriggerTime = Time.time;
        // Play door feedback
        if (audioSource != null && doorSound != null)
        {
            audioSource.PlayOneShot(doorSound);
        }
        // Spawn temporary visual effect
        if (visualEffectPrefab != null)
        {

            GameObject effect = Instantiate( visualEffectPrefab, transform.position, Quaternion.identity );

            Destroy(effect, 2f);
        }
    }
}