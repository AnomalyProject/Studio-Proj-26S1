using UnityEngine;

public class GameSounds : SoundCaller
{
    [SerializeField] AnomalyManager anomalyManager;
    [SerializeField] AudioClip voidTimerTick, voidTimerOver, enteredVoidClip, winGameClip;
    [SerializeField, Min(1)] int warningTicksAtSeconds;
    [SerializeField] private NarrationEvent escapedVoidNarration;

    AudioClip currentMapTrack;
    private bool enteredVoidOnce;

    private void Awake()
    {
        anomalyManager.OnMapChanged += OnMapChanged;
        MapOrientor.OnElevatorInteracted += HandleElevatorInteraction;
        FadeOutMusic(null);
    }

    private void OnDestroy()
    {
        MapOrientor.OnElevatorInteracted -= HandleElevatorInteraction;
    }

    private void HandleElevatorInteraction(LevelExitPoint point, bool arg2) => FadeOutMusic(null);
    private void OnMapChanged(GameMap map)
    {
        currentMapTrack = map.MapMusicTheme;
        if (enteredVoidOnce) escapedVoidNarration.PlayNarration();
    }
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
                enteredVoidOnce = true;
                break;

            case AnomalyManager.RoomState.WinRoom:
                PlaySFXClip(winGameClip);
                break;
        }
    }
    public void OnElevatorFullyOpened()
    {
        PlayMusic(currentMapTrack);
    }
}