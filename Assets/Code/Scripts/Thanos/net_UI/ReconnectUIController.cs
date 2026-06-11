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
    [SerializeField] private Slider loadingSlider;

    [Header("Toast Reference")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text toastText;

    [Header("Settings")]
    [SerializeField] private float sliderSpeed = 2f;

    private IReconnect _networkService;
    private Coroutine _countdownRoutine;
    private Coroutine _toastRoutine;

    //Debug keys to simulate connection events
    private void Update()
    { 
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            HandleConnectionLost();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            HandleReconnected();
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

        ResetUIState();
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

    private void HandleConnectionLost()
    {
        ShowOverlay("Connection Lost", "Reconnecting");
    }

    private void HandleHostMigrating()
    {
        ShowOverlay("Connection Lost", "Host disconnected � migrating session...");
    }

    private void ShowOverlay(string header, string status)
    {
        //Do not show reconnect UI if player is on Menu
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        toastPanel.SetActive(false);
        toastText.text = "";

        overlayPanel.SetActive(true);
        headerText.text = header;
        statusText.text = status;

        HandleInput();

        if (_countdownRoutine != null) StopCoroutine(_countdownRoutine);
        _countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        // changed this because the server should own the reconnect timeout. The UI only displays it.
        float timeLeft = _networkService != null ? _networkService.ReconnectTimeoutSeconds : 30f;

        timeLeft = Mathf.Max(1f, timeLeft);

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

        HandleInput();

        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ShowToastRoutine("Reconnected!"));
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
        ResetUIState();
        
        HandleInput();
        
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

    private void HandleInput()
    {
        if (InputBridge.CurrentContext == InputBridge.InputContext.Player)
        {
            InputBridge.SetContext(InputBridge.InputContext.None);
        }
        else if (InputBridge.CurrentContext == InputBridge.InputContext.None)
        {
            InputBridge.SetContext(InputBridge.InputContext.Player);
        }
    }
}