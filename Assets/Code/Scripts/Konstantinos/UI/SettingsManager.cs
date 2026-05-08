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

    [Header("Categories")]
    [SerializeField] RectTransform CategoryHolderTransform;
    [SerializeField] Vector2[] CatHolderSizeDeltas; // resize holder whenever category changes
    [SerializeField] GameObject[] Categories, CategoryButtonsOutline;
    [SerializeField] int selectedCategory = -1; // -1 = all categories. Then specific category index goes like 0, 1, 2 etc
    private Vector3[] categoryPositions;

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


        InitializeCategories();
    }

    void InitializeCategories()
    {
        categoryPositions = new Vector3[Categories.Length];
        for (int i = 0; i < Categories.Length; i++)
        {
            categoryPositions[i] = Categories[i].transform.localPosition;
        }

        SelectCategory(-1); // enable all
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

    public void SelectCategory(int index)
    {
        selectedCategory = index;

        if (CatHolderSizeDeltas.Length > 0)
        {
            CategoryHolderTransform.sizeDelta = CatHolderSizeDeltas[selectedCategory + 1];
        }

        if (selectedCategory == -1) // all categories
        {
            for (int i = 0; i < Categories.Length; i++)
            {
                Categories[i]?.SetActive(true);
                Categories[i].transform.localPosition = categoryPositions[i];
            }
        }
        else
        {
            foreach (GameObject cat in Categories)
            {
                cat?.SetActive(false);
            }
            Categories[selectedCategory]?.SetActive(true);
            Categories[selectedCategory].transform.localPosition = categoryPositions[0];
        }

        // category buttons outline
        foreach (GameObject btn in CategoryButtonsOutline)
        {
            btn?.SetActive(false);
        }
        CategoryButtonsOutline[selectedCategory + 1]?.SetActive(true);
    }

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
