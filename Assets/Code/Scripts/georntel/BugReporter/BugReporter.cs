using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Net.Mail;
using System;
using TMPro;

public class BugReporter : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject bugReporterPanel;
    public TMP_InputField descriptionInput;
    public TMP_Dropdown frequencyDropdown; 
    public TMP_Dropdown typeDropdown;
    public Button submitButton;
    public Button closeButton;
    public GameObject thankYouMessage;
    public Toggle includeScreenshotToggle;

    [Header("SMTP Configuration")]
    
    public string receiverEmail = "";
    private bool isSending = false;
    private string tempScreenshotPath;

    void Start()
    {
        tempScreenshotPath = Path.Combine(Application.temporaryCachePath, "BugReport_Screenshot.png");

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

        // FOR PROTOTYPE LIVE DEMO. REFACTOR PLZ
        PlayerInput[] playerControls = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (PlayerInput playerInput in playerControls) playerInput.enabled = !isActive;

        if (isActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
       
        //  Screenshot toggle
        if (includeScreenshotToggle != null && includeScreenshotToggle.isOn)
        {
            StopAllCoroutines(); // Stop any ongoing screenshot deletion attempts
            StartCoroutine(CaptureScreenshotAndSend());
        }
        else
        {
            SendEmailReport();
        }
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

        // Restore the Bug Reporter UI 
        bugReporterPanel.SetActive(true);

        // Keep trying to write the screenshot until it's successful
        bool writtenNewScreenshot = false;
        while (!writtenNewScreenshot)
        {
            try
            {
                File.WriteAllBytes(tempScreenshotPath, imageBytes);
                writtenNewScreenshot = true;
            }
            catch { }

            yield return null;
        }
        
        SendEmailReport();
    }

    private void SendEmailReport()
    {
        MailMessage mail = new MailMessage();
        mail.To.Add(receiverEmail);
        mail.Subject = $"{Application.productName} v{Application.version} | Bug Report";
        
        // Harware info
        string hardwareInfo = $"Device Model: {SystemInfo.deviceModel}\n" +
                              $"Device Type: {SystemInfo.deviceType}\n" +
                              $"Resolution: {Screen.currentResolution}\n" +
                              $"CPU: {SystemInfo.processorType}\n" +
                              $"GPU: {SystemInfo.graphicsDeviceName}\n" +
                              $"RAM: {SystemInfo.systemMemorySize} MB";
        
        // Bug info
        string type = typeDropdown.options[typeDropdown.value].text;
        string frequency = frequencyDropdown.options[frequencyDropdown.value].text;
        
        mail.Body = $"Type: {type}\n" +
                    $"Frequency: {frequency}\n\n" +
                    $"Date: {DateTime.Now:yyyy-MM-dd}\n" +
                    $"Time: {DateTime.Now:HH:mm:ss}\n" +
                    $"Version: {Application.version}\n" +
                    $"OS: {SystemInfo.operatingSystem}\n\n" +
                    $"HARDWARE\n{hardwareInfo}\n\n" +
                    $"Player Description:\n{descriptionInput.text}";

        // Attach screenshot only if path is valid
        if (File.Exists(tempScreenshotPath))
        {
            mail.Attachments.Add(new Attachment(tempScreenshotPath));
        }
        
        // Attach log only if path is valid
        if (File.Exists(ExceptionLogger.logFilePath))
        {
            mail.Attachments.Add(new Attachment(ExceptionLogger.logFilePath));
        }

        // We call the static class here
        MailService.SendEmail(
            mail,
            OnMailSuccess, // The success callback
            OnMailFailure  // The failure callback
        );
    }


    // Keep trying to delete the temp screenshot until it's gone
    private IEnumerator DeleteTempScreenshot()
    {
        while (File.Exists(tempScreenshotPath))
        {
            try
            {
                File.Delete(tempScreenshotPath);
            }
            catch { }

            yield return null;
        }
    }

    private void OnMailSuccess()
    {
        isSending = false;
        descriptionInput.text = "";
        
        frequencyDropdown.value = 0;
        typeDropdown.value = 0;
        
        thankYouMessage.SetActive(true);
        Debug.Log("Bug report sent successfully.");
        ValidateInput(descriptionInput.text);
        StartCoroutine(DeleteTempScreenshot());
    }

    private void OnMailFailure(string errorMessage)
    {
        isSending = false;
        Debug.LogError($"Failed to send bug report: {errorMessage}");
        ValidateInput(descriptionInput.text);
        StartCoroutine(DeleteTempScreenshot());
    }
    
    private void CloseReporter()
    {
        bugReporterPanel.SetActive(false);
        thankYouMessage.SetActive(false); 
    }
}