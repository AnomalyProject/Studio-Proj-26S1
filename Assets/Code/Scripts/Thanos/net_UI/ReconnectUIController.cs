using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReconnectUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button returnToMenuButton;

    [Header("Toast Reference")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text toastText;

    [Header("Settings")]
    [SerializeField] private float timeoutDuration = 30f;

    private IReconnectService _networkService;
    private Coroutine _countdownRoutine;

    //@Christina : call this method and add the actual service
    public void InjectDependencies(IReconnectService networkService)
    {
        _networkService = networkService;

        //Events
        _networkService.OnConnectionLost += HandleConnectionLost;
        _networkService.OnHostMigrating += HandleHostMigrating;
        _networkService.OnReconnected += HandleReconnected;

        returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);

        //Default state
        overlayPanel.SetActive(false);
        toastPanel.SetActive(false);
    }

    private void HandleConnectionLost()
    {
        ShowOverlay("Connection Lost", "Attempting to reconnect...");
    }

    //
    private void HandleHostMigrating()
    {
        ShowOverlay("Connection Lost", "Host disconnected — migrating session...");
    }

    private void ShowOverlay(string header, string status)
    {
        overlayPanel.SetActive(true);
        headerText.text = header;
        statusText.text = status;

        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);
        _countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        float timeLeft = timeoutDuration;

        while (timeLeft > 0)
        {
            // Format as 0:XX. Mathf.CeilToInt ensures it doesn't show 0:00 until it's actually done.
            timerText.text = $"Reconnect timeout: 0:{Mathf.CeilToInt(timeLeft):D2}";

            // Use unscaledDeltaTime in case game time is paused (Time.timeScale = 0) during disconnect
            timeLeft -= Time.unscaledDeltaTime;
            yield return null;
        }

        // Timeout expired
        HandleTimeout();
    }

    private void HandleReconnected()
    {
        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);

        overlayPanel.SetActive(false);
        StartCoroutine(ShowToastRoutine("Reconnected!"));
    }

    private IEnumerator ShowToastRoutine(string message)
    {
        toastPanel.SetActive(true);
        toastText.text = message;

        yield return new WaitForSecondsRealtime(2.5f);

        toastPanel.SetActive(false);
    }

    private void HandleTimeout()
    {
        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);

        // Let the network service handle the actual state teardown and scene loading
        _networkService?.CancelAndReturnToMenu();
    }

    private void OnReturnToMenuClicked()
    {
        HandleTimeout();
    }

    private void OnDestroy()
    {
        // Always clean up subscriptions to prevent memory leaks
        if (_networkService != null)
        {
            _networkService.OnConnectionLost -= HandleConnectionLost;
            _networkService.OnHostMigrating -= HandleHostMigrating;
            _networkService.OnReconnected -= HandleReconnected;
        }

        returnToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
    }
}