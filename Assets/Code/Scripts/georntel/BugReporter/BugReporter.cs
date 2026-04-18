using System.IO;
using System.Collections;
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
    public TMP_Dropdown frequencyDropdown; 
    public TMP_Dropdown impactDropdown;
    public Button submitButton;
    public Button closeButton;
    public GameObject thankYouMessage;

    [Header("SMTP Configuration")]
    
    public string receiverEmail = "";
    private bool isSending = false;
    private string tempScreenshotPath;

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
        StartCoroutine(CaptureScreenshotAndSend());
    }
    
    private IEnumerator CaptureScreenshotAndSend()
    {
        // Hide the Bug Reporter UI
        bugReporterPanel.SetActive(false);

        // Wait for the end of the frame so the UI completely disappears
        yield return new WaitForEndOfFrame();

        // Screenshot 
        Texture2D screenImage = ScreenCapture.CaptureScreenshotAsTexture();
        byte[] imageBytes = screenImage.EncodeToPNG();
        Destroy(screenImage); 

        tempScreenshotPath = Path.Combine(Application.temporaryCachePath, "BugReport_Screenshot.png");
        File.WriteAllBytes(tempScreenshotPath, imageBytes);

        // Restore the Bug Reporter UI 
        bugReporterPanel.SetActive(true);
        
        SendEmailReport();
    }

    private void SendEmailReport()
    {
        MailMessage mail = new MailMessage();
        mail.To.Add(receiverEmail);
        mail.Subject = $"{Application.productName} v{Application.version} | Bug Report";
        
        // Harware info
        string hardwareInfo = $"CPU: {SystemInfo.processorType}\n" +
                              $"GPU: {SystemInfo.graphicsDeviceName}\n" +
                              $"RAM: {SystemInfo.systemMemorySize} MB";
        
        // Severity
        string frequency = frequencyDropdown.options[frequencyDropdown.value].text;
        string impact = impactDropdown.options[impactDropdown.value].text;
        
        
        mail.Body = $"Date: {DateTime.Now:yyyy-MM-dd}\n" +
                    $"Time: {DateTime.Now:HH:mm:ss}\n" +
                    $"Version: {Application.version}\n" +
                    $"OS: {SystemInfo.operatingSystem}\n\n" +
                    $"HARDWARE \n{hardwareInfo}\n\n" +
                    $"SEVERITY \n" +
                    $"Frequency: {frequency}\n" +
                    $"Impact: {impact}\n\n" +
                    $"Player Description:\n{descriptionInput.text}";
        
        // Check if the screenshot was captured successfully before trying to attach it
        if (File.Exists(tempScreenshotPath))
        {
            mail.Attachments.Add(new Attachment(tempScreenshotPath));
        }
        
         // Attach
        string logPath = ExceptionLogger.logFilePath; 
        string tempLogPath = Path.Combine(Application.temporaryCachePath, "BugReport_Log.txt");
        
        // Attach only if path is valid
        if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
        {
            // Copy the log file to a temporary location
            File.Copy(logPath, tempLogPath, true);
            mail.Attachments.Add(new Attachment(tempLogPath));
        }

        // We call the static class here
        MailService.SendEmail(
            mail,
            OnMailSuccess, // The success callback
            OnMailFailure  // The failure callback
        );
        
        // CLEAN UP
        mail.Dispose();
        // Delete the temporary files
        if (File.Exists(tempScreenshotPath)) File.Delete(tempScreenshotPath);
        if (File.Exists(tempLogPath)) File.Delete(tempLogPath);
    }

    private void OnMailSuccess()
    {
        isSending = false;
        descriptionInput.text = "";
        
        frequencyDropdown.value = 0;
        impactDropdown.value = 0;
        
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