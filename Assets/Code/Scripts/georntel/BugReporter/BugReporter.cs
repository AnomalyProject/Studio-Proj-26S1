using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Mail;
using System;

public class BugReporter : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject bugReporterPanel;
    public TMP_InputField descriptionInput;
    public Button submitButton;
    public Button closeButton;
    public GameObject thankYouMessage;

    [Header("SMTP Configuration")]
    public string receiverEmail = "";
    
    private bool isSending = false;

    void Start()
    {
        bugReporterPanel.SetActive(false);
        thankYouMessage.SetActive(false);
        submitButton.interactable = false;
        descriptionInput.onValueChanged.AddListener(ValidateInput);
        submitButton.onClick.AddListener(OnSubmitClicked);
        closeButton.onClick.AddListener(CloseReporter);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12)) ToggleReporter();
    }

    public void ToggleReporter()
    {
        bool isActive = !bugReporterPanel.activeSelf;
        bugReporterPanel.SetActive(isActive);
        if (!isActive) thankYouMessage.SetActive(false);
    }

    private void ValidateInput(string input)
    {
        submitButton.interactable = !string.IsNullOrWhiteSpace(input) && !isSending;
    }

    private void OnSubmitClicked()
    {
        if (isSending) return;
        isSending = true;
        submitButton.interactable = false; 

        SendEmailReport();
    }

    private void SendEmailReport()
    {
        MailMessage mail = new MailMessage();
        mail.To.Add(receiverEmail);
        mail.Subject = $"{Application.productName} | Bug Report";
        mail.Body = $"Date: {DateTime.Now:yyyy-MM-dd}\n" +
                    $"Time: {DateTime.Now:HH:mm:ss}\n" +
                    $"OS: {SystemInfo.operatingSystem}\n\n" +
                    $"Player Description:\n{descriptionInput.text}";

        // We call the static class here
        MailService.SendEmail(
            mail,
            OnMailSuccess, // The success callback
            OnMailFailure  // The failure callback
        );
    }

    private void OnMailSuccess()
    {
        isSending = false;
        descriptionInput.text = "";
        thankYouMessage.SetActive(true);
        Debug.Log("Bug report sent successfully.");
        ValidateInput(descriptionInput.text);
    }

    private void OnMailFailure(string errorMessage)
    {
        isSending = false;
        Debug.LogError($"Failed to send bug report: {errorMessage}");
        ValidateInput(descriptionInput.text);
    }

    private void CloseReporter()
    {
        bugReporterPanel.SetActive(false);
        thankYouMessage.SetActive(false); 
    }
}