using UnityEngine;

public class SessionSounds : SoundCaller
{
    [SerializeField] private AudioClip playerJoinedClip, playerLeftClip;

    private void Awake()
    {
        SessionEvents.OnPlayerJoined += HandlePlayerJoinedEvent;
        SessionEvents.OnPlayerLeft += HandlePlayerLeftEvent;
    }

    private void OnDestroy()
    {
        SessionEvents.OnPlayerJoined -= HandlePlayerJoinedEvent;
        SessionEvents.OnPlayerLeft -= HandlePlayerLeftEvent;
    }

    private void HandlePlayerJoinedEvent(ulong arg1, string arg2) => PlayUIClip(playerJoinedClip);
    private void HandlePlayerLeftEvent(ulong arg1, string arg2) => PlayUIClip(playerLeftClip);
}