using UnityEngine;

public class GameSounds : SoundCaller
{
    [SerializeField] AnomalyManager anomalyManager;
    [SerializeField] AudioClip voidTimerTick, voidTimerOver, enteredVoidClip, winGameClip;
    [SerializeField, Min(1)] int warningTicksAtSeconds;
    public void OnVoidTimerOver() => PlaySFXClip(voidTimerOver);
    public void OnVoidTimerTick(float currentTime)
    {
        if(currentTime <= warningTicksAtSeconds && currentTime != 0)
        PlayUIClip(voidTimerTick);
    }

    public void OnElevatorOpened()
    {
        switch (anomalyManager.CurrentState)
        {
            case AnomalyManager.RoomState.PunishmentRoom:
                PlaySFXClip(enteredVoidClip);
                break;

            case AnomalyManager.RoomState.WinRoom:
                PlaySFXClip(winGameClip);
                break;
        }
    }
}