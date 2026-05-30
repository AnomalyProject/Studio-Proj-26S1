using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    // Singleton reference
    public static SettingsManager Instance { get; private set; }

    private bool Initialized; // for running pieces of code ONLY after everything is initialized 

    // Events
    public static event Action OnSettingsOpened;
    public static event Action OnSettingsClosed;

    [Header("Categories")]
    [SerializeField] RectTransform CategoryHolderTransform; // holds all category tabs
    [SerializeField] Vector2[] CatHolderSizeDeltas; // resize holder whenever category changes
    [SerializeField] GameObject[] Categories, CategoryButtonsOutline; // Category tabs & filter button outlines
    [SerializeField] int selectedCategory = -1; // -1 = all categories. Then specific category index goes like 0, 1, 2 etc
    private float[] categoryYPositions;

    [Header("World References")]
    [SerializeField] Canvas settingsCanvas;
    [SerializeField] GameObject firstSelectedElement;
    private Vector3 WorldPosition, WorldEulerAngles, WorldScale;
    public Vector2 WorldAnchorMin, WorldAnchorMax, WorldAnchoredPosition, WorldSizeDelta, WorldPivot;
    private bool capturedTransform;

    [Header("Settings Controls References")]
    // Audio
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider SFXVolumeSlider;
    [SerializeField] Slider UIVolumeSlider;

    // Graphics
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] Toggle vSyncToggle;
    [SerializeField] Toggle fullscreenToggle;

    // Controls
    [SerializeField] Slider lookSensitivitySlider;
    [SerializeField] Toggle invertLookXToggle, invertLookYToggle;

    //------------------------------------------------------------//

    // Saveable variables: 

    // Audio
    private float masterVolume = 1.0f, musicVolume = 0.5f, SFXVolume = 1.0f, UIVolume = 1.0f;

    // Graphics 
    private int resolutionValue;
    private bool vSync, fullscreen;

    //------------------------------------------------------------//

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
    }

    private void Start()
    {
        // initialization happens on Start() because it depends on other Systems to be singleton first like the AudioManager
        Close();
        InitializeSettings();
    }

    #region Initializations
    public void InitializeSettings()
    {
        InitializeCategories();
        InitializeAudio();
        InitializeGraphics();
        InitializeControls();
        InitializeAccessibility();

        Initialized = true;
    }

    void InitializeCategories()
    {
        categoryYPositions = new float[Categories.Length];
        for (int i = 0; i < Categories.Length; i++)
        {
            categoryYPositions[i] = Categories[i].transform.localPosition.y; // store y position for later repositioning
        }

        SelectCategory(-1); // enable all
    }

    void InitializeAudio()
    {
        // Get any saved data
        if (PlayerPrefs.HasKey("Master Volume")) masterVolume = PlayerPrefs.GetFloat("Master Volume");
        if (PlayerPrefs.HasKey("Music Volume")) musicVolume = PlayerPrefs.GetFloat("Music Volume");
        if (PlayerPrefs.HasKey("SFX Volume")) SFXVolume = PlayerPrefs.GetFloat("SFX Volume");
        if (PlayerPrefs.HasKey("UI Volume")) UIVolume = PlayerPrefs.GetFloat("UI Volume");

        // Apply the values on the Sliders (OnValueChanged gets called)
        masterVolumeSlider.value = masterVolume;
        musicVolumeSlider.value = musicVolume;
        SFXVolumeSlider.value = SFXVolume;
        UIVolumeSlider.value = UIVolume;
    }

    void InitializeGraphics()
    {
        // Initialize Resolutions
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        // Get all supported resolutions
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            string option =
                $"{Screen.resolutions[i].width} x {Screen.resolutions[i].height} @ {Screen.resolutions[i].refreshRateRatio.value:F0}Hz";

            options.Add(option);

            // Match current screen resolution
            if (Screen.resolutions[i].width == Screen.currentResolution.width &&
                Screen.resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        // add all supported resolutions as options on the dropdown
        resolutionDropdown.AddOptions(options);

        if (PlayerPrefs.HasKey("Resolution"))
            currentResolutionIndex = PlayerPrefs.GetInt("Resolution"); // apply the last resolution if previously changed

        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionValue = currentResolutionIndex;



        // Initialize V-Sync
        if (PlayerPrefs.HasKey("V-Sync")) vSync = bool.Parse(PlayerPrefs.GetString("V-Sync"));
        else vSync = true;

        vSyncToggle.isOn = vSync;



        // Initialize FullScreen
        if (PlayerPrefs.HasKey("Fullscreen")) fullscreen = bool.Parse(PlayerPrefs.GetString("Fullscreen"));
        else fullscreen = true; 

        fullscreenToggle.isOn = fullscreen;
    }

    void InitializeControls()
    {
        // Initialize Look Sensitivity
        lookSensitivitySlider.value = InputBridge.Sensitivity;
        
        // Initialize Invert Look
        invertLookXToggle.isOn = InputBridge.invertX;
        invertLookYToggle.isOn = InputBridge.invertY;
    }

    void InitializeAccessibility()
    {
        // WIP
    }
    #endregion

    // Static method, opens canvas, SettingsManager.Open()
    public static void Open()
    {
        Instance?.settingsCanvas.gameObject.SetActive(true);
        IsOpen = true;
        EventSystem.current.SetSelectedGameObject(Instance.firstSelectedElement);
        OnSettingsOpened?.Invoke();

        if (Instance.Initialized)
        {
            Instance?.SelectCategory(-1); // selects all
        }
        else
        {
            Instance?.InitializeSettings();
        }
    }

    // Static method, closes canvas, SettingsManager.Close()
    public static void Close()
    {
        Instance?.settingsCanvas.gameObject.SetActive(false);
        IsOpen = false;
        EventSystem.current.SetSelectedGameObject(null);
        OnSettingsClosed?.Invoke();

        if (Instance.Initialized)
        {
            Instance.SelectCategory(-1);
        }
    }

    public static bool IsOpen { get; private set; }

    public void SwitchCanvasMode(RenderMode renderMode)
    {
        settingsCanvas.renderMode = renderMode;

        if (renderMode == RenderMode.WorldSpace)
        {
            // restore settings canvas world transform
            RectTransform worldTransform = settingsCanvas.GetComponent<RectTransform>();

            worldTransform.anchorMin = WorldAnchorMin;
            worldTransform.anchorMax = WorldAnchorMax;
            worldTransform.anchoredPosition = WorldAnchoredPosition;
            worldTransform.sizeDelta = WorldSizeDelta;
            worldTransform.pivot = WorldPivot;

            worldTransform.position = WorldPosition;
            worldTransform.eulerAngles = WorldEulerAngles;
            worldTransform.localScale = WorldScale;
        }
    }

    public void CaptureWorldTransform()
    {
        if (capturedTransform) return;
        
        capturedTransform = true;

        RectTransform worldTransform = settingsCanvas.GetComponent<RectTransform>();

        WorldAnchorMin = worldTransform.anchorMin;
        WorldAnchorMax = worldTransform.anchorMax;
        WorldAnchoredPosition = worldTransform.anchoredPosition;
        WorldSizeDelta = worldTransform.sizeDelta;
        WorldPivot = worldTransform.pivot;

        WorldPosition = worldTransform.position;
        WorldEulerAngles = worldTransform.eulerAngles;
        WorldScale = worldTransform.localScale;
    }

    // filters and shows a specific category
    public void SelectCategory(int index)
    {
        selectedCategory = index;

        if (CatHolderSizeDeltas.Length > 0)
        {
            // resize holder so there's no dead scroll space for each category
            CategoryHolderTransform.sizeDelta = CatHolderSizeDeltas[selectedCategory + 1];
        }

        if (selectedCategory == -1) // all categories
        {
            for (int i = 0; i < Categories.Length; i++)
            {
                Categories[i]?.SetActive(true);

                // each category moves to their original positions
                Categories[i].transform.localPosition = new Vector3(Categories[i].transform.localPosition.x, categoryYPositions[i], 0);
            }
        }
        else // specific category
        {
            foreach (GameObject cat in Categories)
            {
                cat?.SetActive(false);
            }
            Categories[selectedCategory]?.SetActive(true);

            // move our category to the top
            Categories[selectedCategory].transform.localPosition = new Vector3(Categories[selectedCategory].transform.localPosition.x, categoryYPositions[0], 0);
        }

        // category buttons outline
        foreach (GameObject btn in CategoryButtonsOutline)
        {
            btn?.SetActive(false);
        }
        CategoryButtonsOutline[selectedCategory + 1]?.SetActive(true);
    }

    //------------------------------------------------------------//

    #region Options Change Methods

    // Audio
    public void OnMasterVolumeChanged(float value)
    {
        masterVolume = value;

        // Convert linear slider position into logarithmic response
        float logarithmicValue = Mathf.Log10(Mathf.Lerp(0.0001f, 1f, masterVolume)) + 1f;

        AudioManager.Instance.Volume(AudioManager.AudioChannel.Master, logarithmicValue);

        PlayerPrefs.SetFloat("Master Volume", masterVolume); // save linear value so we can call the method with it
    }

    public void OnMusicVolumeChanged(float value)
    {
        musicVolume = value;

        // Convert linear slider position into logarithmic response
        float logarithmicValue = Mathf.Log10(Mathf.Lerp(0.0001f, 1f, value)) + 1f;

        AudioManager.Instance.Volume(AudioManager.AudioChannel.Music, logarithmicValue);

        PlayerPrefs.SetFloat("Music Volume", musicVolume); // save linear value so we can call the method with it
    }

    public void OnSFXVolumeChanged(float value)
    {
        SFXVolume = value;

        // Convert linear slider position into logarithmic response
        float logarithmicValue = Mathf.Log10(Mathf.Lerp(0.0001f, 1f, value)) + 1f;

        AudioManager.Instance.Volume(AudioManager.AudioChannel.SFX, logarithmicValue);

        PlayerPrefs.SetFloat("SFX Volume", SFXVolume); // save linear value so we can call the method with it
    }

    public void OnUIVolumeChanged(float value)
    {
        UIVolume = value;

        // Convert linear slider position into logarithmic response
        float logarithmicValue = Mathf.Log10(Mathf.Lerp(0.0001f, 1f, value)) + 1f;

        AudioManager.Instance.Volume(AudioManager.AudioChannel.UI, logarithmicValue);

        PlayerPrefs.SetFloat("UI Volume", UIVolume); // save linear value so we can call the method with it
    }

    //------------------------------------------------------------//
    // Graphics
    public void OnResolutionChanged(int value)
    {
        resolutionValue = value;

        Resolution selectedResolution = Screen.resolutions[value];

        Debug.Log(
            $"[SettingsManager] Resolution changed to " +
            $"{selectedResolution.width}x{selectedResolution.height}"
        );

        Screen.SetResolution(
          selectedResolution.width,
          selectedResolution.height,
          Screen.fullScreenMode,
          selectedResolution.refreshRateRatio
        );

        PlayerPrefs.SetInt("Resolution", resolutionValue);
    }

    public void ToggleVSync(bool value)
    {
        vSync = value;

        Debug.Log($"[SettingsManager] V-Sync changed to {vSync}");

        if (value) QualitySettings.vSyncCount = 1;
        else QualitySettings.vSyncCount = 0;

        PlayerPrefs.SetString("V-Sync", vSync.ToString());
    }

    public void ToggleFullscreen(bool value)
    {
        fullscreen = value;

        Debug.Log($"[SettingsManager] Fullscreen changed to {fullscreen}");

        Screen.fullScreen = fullscreen;

        PlayerPrefs.SetString("Fullscreen", vSync.ToString());
    }

    //------------------------------------------------------------//
    // Controls
    public void OnLookSensitivityChanged(float value)
    {
        Debug.Log($"[SettingsManager] Look Sensitivity changed to {value}");
        InputBridge.ChangeSensitivity(value);
    }

    public void ToggleInvertXLook(bool value)
    {
        Debug.Log($"[SettingsManager] Invert Look X changed to {value}");
        InputBridge.ChangeInvertX(value);
    }

    public void ToggleInvertYLook(bool value)
    {
        Debug.Log($"[SettingsManager] Invert Look Y changed to {value}");
        InputBridge.ChangeInvertY(value);
    }
    #endregion
}
