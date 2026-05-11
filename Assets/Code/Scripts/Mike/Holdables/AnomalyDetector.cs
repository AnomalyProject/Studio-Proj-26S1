using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class AnomalyDetector : PlayerItem, IInteractable<PlayerBody>
{
    [Serializable] struct DisplayInfo
    {
        public Color color;
        public string msg;
        public AudioClip clip;
        [Min(0)] public float waitTime;
    }

    [SerializeField] private Image bg;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] DisplayInfo anomalyFoundDisplay, areaClearDisplay, analyzingDisplay, 
        inactiveDisplay,consumedDisplay, unavailableDisplay;

    AudioSource audioSource;

    void Awake() => audioSource = GetComponent<AudioSource>();

    private void OnEnable()
    {
        if (InValidArea()) SetVisuals(inactiveDisplay);
        else SetVisuals(unavailableDisplay);
    }

    public Task<bool> CanInteract(PlayerBody interactor) => Task.FromResult(true);
    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        if (!InValidArea())
        {
            await AwaitableDisplay(unavailableDisplay);
            return false;
        }

        await PerformAreaScan();
        return true;
    }

    private async Task PerformAreaScan()
    {
        await AwaitableDisplay(analyzingDisplay);

        bool hasAnomaly = RefrenceManager.Instance.Gameplay.AnomalyManager.HasAnomaly;

        if (hasAnomaly) await AwaitableDisplay(anomalyFoundDisplay);
        else await AwaitableDisplay(areaClearDisplay);

        await AwaitableDisplay(consumedDisplay);
    }

    private bool InValidArea()
    {
        var currentState = RefrenceManager.Instance.Gameplay.AnomalyManager.CurrentState;

        switch (currentState)
        {
            case AnomalyManager.RoomState.PunishmentRoom:
            case AnomalyManager.RoomState.WinRoom:
                return false;
        }

        return true;
    }

    private async Task AwaitableDisplay(DisplayInfo info)
    {
        SetVisuals(info);
        if (info.clip != null) audioSource.PlayOneShot(info.clip);
        int waitMilliseconds = Mathf.RoundToInt(info.waitTime * 1000);
        await Task.Delay(waitMilliseconds);
    }

    private void SetVisuals(DisplayInfo info)
    {
        bg.color = info.color;
        displayText.text = info.msg;
    }
}