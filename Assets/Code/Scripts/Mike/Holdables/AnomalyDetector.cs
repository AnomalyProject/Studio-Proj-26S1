using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class AnomalyDetector : PlayerItem, IInteractable<PlayerBody>
{
    [SerializeField] private Image bg;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField, Min(1)] private int waitSeconds = 2;
    [SerializeField] Color inactiveColor, analyzingColor, anomalyColor, clearAreaColor;
    [SerializeField] AudioClip anomalyFoundClip, areaClearClip, scanPerformClip;

    AudioSource audioSource;

    void Awake() => audioSource = GetComponent<AudioSource>();

    private void OnEnable()
    {
        if (InValidArea()) SetDisplay(inactiveColor, "Awaiting User Input...");
        else SetDisplay(inactiveColor, "No Signal...");
    }

    public Task<bool> CanInteract(PlayerBody interactor) => Task.FromResult(true);
    public async Task<bool> TryInteract(PlayerBody interactor)
    {

        if (!InValidArea())
        {
            SetDisplay(inactiveColor, "No Signal...");
            return false;
        }

        await PerformAreaScan();
        return true;
    }

    private async Task PerformAreaScan()
    {
        SetDisplay(analyzingColor, "Analyzing Area...", scanPerformClip);
        await Task.Delay(waitSeconds * 1000);

        bool hasAnomaly = RefrenceManager.Instance.Gameplay.AnomalyManager.HasAnomaly;

        if (hasAnomaly) SetDisplay(anomalyColor, "Anomalies Spotted!", anomalyFoundClip);
        else SetDisplay(clearAreaColor, "Area Clear.", areaClearClip);

        await Task.Delay(waitSeconds * 1000);
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

    private void SetDisplay(Color color, string msg, AudioClip clip = null)
    {
        if (clip != null) audioSource.PlayOneShot(clip);

        bg.color = color;
        displayText.text = msg;
    }
}