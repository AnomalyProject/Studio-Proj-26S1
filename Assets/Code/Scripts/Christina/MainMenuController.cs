using UnityEngine;
using Steamworks;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject coopPanel;
    [SerializeField] private GameObject joinPanel;

    public void OnStartPressed()
    {
        startPanel.SetActive(false);
        modePanel.SetActive(true);
    }

    public void OnSinglePlayerPressed()
    {
        SessionModeManager.Instance.StartSolo();
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
    
    public void OnJoinCoOpPressed()
    {
        coopPanel.SetActive(false);
        joinPanel.SetActive(true);
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
        joinPanel.SetActive(false);
        modePanel.SetActive(true);
    }
    
    public void OnBackToCoOpPressed()
    {
        joinPanel.SetActive(false);
        coopPanel.SetActive(true);
    }
    
}