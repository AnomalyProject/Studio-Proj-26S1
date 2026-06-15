using UnityEngine;
using System;
using TMPro;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Timer for the Void area.
/// </summary>
public class VoidTimer : MonoBehaviour
{
    private TextMeshProUGUI timerText;
    private Color defaultColor;

    private void Start()
    {
        timerText = GetComponentInChildren<TextMeshProUGUI>(true);
        defaultColor = timerText.color;
        GameManager.OnInitialized += InitManager;
        GameManager.OnDestroyed += HandleManagerDestruction;

        TutorialManager.OnInitialized += InitManager;
        TutorialManager.OnDestroyed += HandleManagerDestruction;
    }

    private void OnDestroy()
    {
        GameManager.OnInitialized -= InitManager;
        GameManager.OnDestroyed -= HandleManagerDestruction;
    }
    private void HandleManagerDestruction(GameManager gameManager) => ShowTimer(false);
    private void HandleManagerDestruction(TutorialManager tutorialManager) => ShowTimer(false);

    private void InitManager(GameManager gameManager)
    {
        if (gameManager == null) return;

        gameManager.AnomalyManager.OnStateChanged += (state) =>
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
    private void InitManager(TutorialManager tutorialManager)
    {
        if (tutorialManager == null) return;

        tutorialManager.OnVoidTimerExpired.AddListener(() =>
        {
            timerText.color = Color.red;
            //Debug.Log("Reward with achievement here");
        });
        tutorialManager.OnVoidExitButtonPressed.AddListener(() => ShowTimer(false));
        tutorialManager.OnVoidTimerTick.AddListener(TickTimer);
        tutorialManager.AfterEnteringVoid.AddListener(() =>
        {
            timerText.text = string.Empty;
            timerText.color = defaultColor;
            ShowTimer(true);
        });
    }
    private void TickTimer(float time) => timerText.text = TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
    private void ShowTimer(bool show) => timerText.gameObject.SetActive(show);
}
