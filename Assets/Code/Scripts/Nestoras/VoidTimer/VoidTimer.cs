using UnityEngine;
using System;
using TMPro;

public class VoidTimer : MonoBehaviour
{
    private TextMeshProUGUI timerText;
    private AnomalyManager anomalyManager;
    private GameManager gameManager;
    private Color defaultColor;

    private void Awake()
    {
        timerText = GetComponentInChildren<TextMeshProUGUI>(true);
        gameManager = FindFirstObjectByType<GameManager>();
        anomalyManager = FindFirstObjectByType<AnomalyManager>();

        defaultColor = timerText.color;

        anomalyManager.OnStateChanged += (state) =>
        {
            if (state != AnomalyManager.RoomState.PunishmentRoom) ShowTimer(false); 
        };
        gameManager.OnPunishmentTimerExpired.AddListener(() => timerText.color = Color.red);

        gameManager.OnWrongDecision.AddListener(() =>
        {
            timerText.text = string.Empty;
            timerText.color = defaultColor;
            ShowTimer(true);
        });

        gameManager.OnPunishmentTimerTick.AddListener(TickTimer);

        gameManager.OnGameReset.AddListener(() => ShowTimer(false));
        gameManager.OnGameWon.AddListener(() => ShowTimer(false));
    }

    private void TickTimer(float time) => timerText.text = TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
    private void ShowTimer(bool show) => timerText.gameObject.SetActive(show);
}
