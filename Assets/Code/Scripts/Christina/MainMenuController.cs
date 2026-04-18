using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject coopPanel;
    [SerializeField] private string soloGameplayScene = "NetworkTestScene";

    public void OnStartPressed()
    {
        startPanel.SetActive(false);
        modePanel.SetActive(true);
    }

    public void OnSinglePlayerPressed()
    {
        SessionModeManager.Instance.StartSolo(soloGameplayScene);
    }

    public void OnMultiplayerPressed()
    {
        modePanel.SetActive(false);
        coopPanel.SetActive(true);
    }

    public void OnHostCoOpPressed()
    {
        SessionModeManager.Instance.StartHosting();
    }

    public void OnBackToStartPressed()
    {
        modePanel.SetActive(false);
        coopPanel.SetActive(false);
        startPanel.SetActive(true);
    }

    public void OnBackToModePressed()
    {
        coopPanel.SetActive(false);
        modePanel.SetActive(true);
    }
}