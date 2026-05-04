using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject coopPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;

    private void OnEnable()
    {
        string message = SessionModeManager.Instance.LastJoinFailureMessage;

        if (!String.IsNullOrWhiteSpace(message))
        {
            ShowMessage(message);
            SessionModeManager.Instance.ClearLastJoinFailureMessage();
        }
    }

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

    private void ShowMessage(string message)
    {
        if (messagePanel == null || messageText == null) return;
        
        messagePanel.SetActive(true);
        messageText.text = message;
    }
    
    //todo: we should use MainMenuManager for these actions
    public void OnQuitGame()
    {
        Application.Quit();
        Debug.Log("Quit");
    }
    
}