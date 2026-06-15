using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ReconnectUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text timerText;

    [SerializeField] private Slider fillSlider;
    [SerializeField] private Slider backgroundSlider;

    [Header("Toast Reference")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text toastText;

    [Header("Settings")]
    [SerializeField] private float sliderSpeed = 1f;

    private IReconnect _networkService;
    private IAfk _afkService;
    private Coroutine _countdownRoutine;
    private Coroutine _toastRoutine;

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha9)) HandleConnectionLost();
        if (Input.GetKeyDown(KeyCode.Alpha0)) HandleReconnected();
        if (Input.GetKeyDown(KeyCode.Alpha8)) HandleAfkDetected();   // debug: simulate AFK
#endif
    }

    public void InjectDependencies(IReconnect networkService)
    {
        _networkService = networkService;

        _networkService.OnConnectionLost += HandleConnectionLost;
        _networkService.OnHostMigrating += HandleHostMigrating;
        _networkService.OnReconnected += HandleReconnected;
        _networkService.OnReconnectFailed += HandleReconnectFailed;

        ResetUIState();
    }
    public void InjectAfkDependencies(IAfk afkService)
    {
        _afkService = afkService;

        _afkService.OnAfkDetected += HandleAfkDetected;
        _afkService.OnAfkCancelled += HandleAfkCancelled;
    }
    private void HandleConnectionLost()
    {
        ShowOverlay("Connection Lost", "Reconnecting");
    }

    private void HandleHostMigrating()
    {
        ShowOverlay("Connection Lost", "Host disconnected — migrating session...");
    }
    private void HandleReconnected()
    {
        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);

        overlayPanel.SetActive(false);
        RestoreGameplayInput();

        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ShowToastRoutine("Reconnected!"));
    }

    private void HandleReconnectFailed(string reason)
    {
        ResetUIState();
        RestoreGameplayInput();
    }
    private void HandleAfkDetected()
    {
        ShowOverlay("Connection Lost", "Reconnecting");
        RestoreGameplayInput();
    }

    private void HandleAfkCancelled()
    {
        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);

        overlayPanel.SetActive(false);

        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ShowToastRoutine("Welcome back!"));
    }
    private void ShowOverlay(string header, string status)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        toastPanel.SetActive(false);
        toastText.text = "";

        overlayPanel.SetActive(true);
        headerText.text = header;
        statusText.text = status;

        LockReconnectInput();

        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);
        _countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        float timeLeft = _networkService != null ? _networkService.ReconnectTimeoutSeconds : 30f;
        timeLeft = Mathf.Max(1f, timeLeft);

        float marqueeAge = 0f;
        float initialFill = 0.3f;

        fillSlider.value = initialFill;

        while (timeLeft > 0)
        {
            timerText.text = $"Reconnect timeout: 0:{Mathf.CeilToInt(timeLeft):D2}";

            if (fillSlider != null && backgroundSlider != null)
            {
                marqueeAge += Time.unscaledDeltaTime * sliderSpeed;

                float cycleProgress = marqueeAge % 2f;

                if (cycleProgress < 1f)
                {
                    fillSlider.value = cycleProgress + initialFill;
                    backgroundSlider.value = cycleProgress;
                }
                else
                {
                    fillSlider.value = (cycleProgress - 1f) + initialFill;
                    backgroundSlider.value = cycleProgress - 1f;
                }
            }

            timeLeft -= Time.unscaledDeltaTime;
            yield return null;
        }

        HandleTimeout();
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
        Debug.Log("[ReconnectUI] Timeout reached. Returning to menu.");

        ResetUIState();
        RestoreGameplayInput();
        _networkService?.CancelAndReturnToMenu();
    }

    public void OnReturnToMenuClicked()
    {
        HandleTimeout();
    }
    private void ResetUIState()
    {
        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);

        overlayPanel.SetActive(false);
        toastPanel.SetActive(false);

        timerText.text = "";
        toastText.text = "";
    }

    private void LockReconnectInput()
    {
        InputBridge.SetContext(InputBridge.InputContext.None);
    }

    private void RestoreGameplayInput()
    {
        InputBridge.SetContext(InputBridge.InputContext.Player);
    }
    private void OnDestroy()
    {
        if (_networkService != null)
        {
            _networkService.OnConnectionLost -= HandleConnectionLost;
            _networkService.OnHostMigrating -= HandleHostMigrating;
            _networkService.OnReconnected -= HandleReconnected;
            _networkService.OnReconnectFailed -= HandleReconnectFailed;
        }

        if (_afkService != null)
        {
            _afkService.OnAfkDetected -= HandleAfkDetected;
            _afkService.OnAfkCancelled -= HandleAfkCancelled;
        }
    }
}