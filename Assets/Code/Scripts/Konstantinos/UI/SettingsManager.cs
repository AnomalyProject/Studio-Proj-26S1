using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // Events
    public static event Action OnSettingsOpened;
    public static event Action OnSettingsClosed;

    [Header("World References")]
    [SerializeField] GameObject settingsCanvas;
    [SerializeField] GameObject firstSelectedElement;

    [Header("Settings Controls References")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private void Awake()
    {
        // singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);

        //initialization
        settingsCanvas.SetActive(false);
        InitializeSettings();
    }

    public void InitializeSettings()
    {
        // Makes sure the slider/dropdown/toggle values are correct

        //masterVolumeSlider.value = AudioManager.Instance.
        //resolutionDropdown.value = 
    }

    public static void Open()
    {
        Instance?.settingsCanvas.SetActive(true);
        Instance?.InitializeSettings();
        IsOpen = true;
        EventSystem.current.SetSelectedGameObject(Instance.firstSelectedElement);
        OnSettingsOpened?.Invoke();
    }

    public static void Close()
    {
        Instance?.settingsCanvas.SetActive(false);
        IsOpen = false;
        EventSystem.current.SetSelectedGameObject(null);
        OnSettingsClosed?.Invoke();
    }

    public static bool IsOpen { get; private set; }

    public void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance.Volume(AudioManager.AudioChannel.Master, value);
    }

    public void OnResolutionChanged(int value)
    {
        Debug.Log($"[from: SettingsManager] Resolution has changed to {value}");
        // TODO: Hook into resolution/display system when available
    }
}
