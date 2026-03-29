using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider progressBar;

    private void Awake()
    {
        // singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // disable canvas on awake
        HideUI();

        DontDestroyOnLoad(gameObject);
    }

    public void ShowUI()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
    }

    public void HideUI()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    public void SetProgress(float value)
    {
        if (progressBar != null)
            progressBar.value = value;
    }
}