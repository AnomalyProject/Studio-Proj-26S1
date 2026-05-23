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
    [SerializeField] private Slider loadingSlider;

    [Header("Toast Reference")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text toastText;

    [Header("Settings")]
    [SerializeField] private float timeoutDuration = 30f;
    [SerializeField] private float sliderSpeed = 2f;

    private IReconnect _networkService;
    private Coroutine _countdownRoutine;

    //Debug keys to simulate connection events
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            HandleConnectionLost();
        }
    }
    //@Christina : call this method and add the actual service
    public void InjectDependencies(IReconnect networkService)
    {
        _networkService = networkService;

        //Events
        _networkService.OnConnectionLost += HandleConnectionLost;
        _networkService.OnHostMigrating += HandleHostMigrating;
        _networkService.OnReconnected += HandleReconnected;

        //Default state
        overlayPanel.SetActive(false);
        toastPanel.SetActive(false);
    }

    private void HandleConnectionLost()
    {
        ShowOverlay("Connection Lost", "Attempting to reconnect...");
    }

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
            timerText.text = $"Reconnect timeout: 0:{Mathf.CeilToInt(timeLeft):D2}";

            if (loadingSlider != null)
            {
                loadingSlider.value = Mathf.PingPong(Time.unscaledTime * sliderSpeed, 1f);
            }

            timeLeft -= Time.unscaledDeltaTime;
            yield return null;
        }

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

        _networkService?.CancelAndReturnToMenu();
    }

    public void OnReturnToMenuClicked()
    {
        HandleTimeout();
    }

    private void OnDestroy()
    {
        if (_networkService != null)
        {
            _networkService.OnConnectionLost -= HandleConnectionLost;
            _networkService.OnHostMigrating -= HandleHostMigrating;
            _networkService.OnReconnected -= HandleReconnected;
        }
    }
}