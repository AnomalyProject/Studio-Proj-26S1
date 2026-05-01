using UnityEngine;

public class GameSounds : SoundCaller
{
    [SerializeField] AudioClip voidTimerTick, voidTimerOver;
    [SerializeField, Min(1)] int warningTicksAtSeconds;

    public void OnVoidTimerOver() => PlayUIClip(voidTimerOver);
    public void OnVoidTimerTick(float currentTime)
    {
        if(currentTime <= warningTicksAtSeconds && currentTime != 0)
        PlayUIClip(voidTimerTick);
    }
}