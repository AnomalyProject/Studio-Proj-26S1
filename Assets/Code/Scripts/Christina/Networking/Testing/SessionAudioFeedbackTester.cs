using UnityEngine;

public class SessionAudioFeedbackTester : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip playerJoinedClip;
    [SerializeField] private AudioClip playerLeftClip;
    
    private static SessionAudioFeedbackTester instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void OnEnable()
    {
        SessionEvents.OnPlayerJoined += PlayPlayerJoinedSound;
        SessionEvents.OnPlayerLeft += PlayPlayerLeftSound;
    }

    private void OnDisable()
    {
        SessionEvents.OnPlayerJoined -= PlayPlayerJoinedSound;
        SessionEvents.OnPlayerLeft -= PlayPlayerLeftSound;
    }
    
    private void PlayPlayerJoinedSound(ulong steamID, string displayName)
    {
        Debug.Log($"[AudioTest] Player joined: {displayName}");

        audioSource.PlayOneShot(playerJoinedClip);
    }

    private void PlayPlayerLeftSound(ulong steamID, string reason)
    {
        Debug.Log($"[AudioTest] Player left: {steamID}. Reason: {reason}");

        audioSource.PlayOneShot(playerLeftClip);
    }
}
