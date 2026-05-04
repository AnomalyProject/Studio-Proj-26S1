using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using System.Net.Mail;
using System.IO;
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

    [Header("SMTP Configuration")]
    
    public string receiverEmail = "";
    private bool isSending = false;
    private string tempScreenshotPath;
    private string tempLogPath;

    private void Awake()
    {
        InputBridge.OnContextChanged += ToggleReporter;
    }
    private void OnDestroy()
    {
        InputBridge.OnContextChanged -= ToggleReporter;
    }

    private void Start()
    {
        tempScreenshotPath = Path.Combine(Application.temporaryCachePath, "Screenshot.png");
        tempLogPath = Path.Combine(Application.temporaryCachePath, "Console.log");

        bugReporterPanel.SetActive(false);
        thankYouMessage.SetActive(false);
        submitButton.interactable = false;
        descriptionInput.onValueChanged.AddListener(ValidateInput);
        submitButton.onClick.AddListener(OnSubmitClicked);
        closeButton.onClick.AddListener(() => CloseReporter(new InputAction.CallbackContext()));
    }

    public void ToggleReporter(InputBridge.InputContext context)
    {
        bool isActive = context == InputBridge.InputContext.BugReporter;
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
        StopAllCoroutines(); 
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
            File.Copy(ExceptionLogger.logFilePath, tempLogPath, true);
            mail.Attachments.Add(new Attachment(tempLogPath));
        }

        // We call the static class here
        MailService.SendEmail(
            mail,
            OnMailSuccess, // The success callback
            OnMailFailure  // The failure callback
        );
    }


    // Keep trying to delete the temp files until they're gone
    private IEnumerator DeleteTempFiles()
    {
        while (File.Exists(tempScreenshotPath))
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                File.Delete(tempScreenshotPath);
                File.Delete(tempLogPath);
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
        StartCoroutine(DeleteTempFiles());
    }

    private void OnMailFailure(string errorMessage)
    {
        isSending = false;
        Debug.LogError($"Failed to send bug report: {errorMessage}");
        ValidateInput(descriptionInput.text);
        StartCoroutine(DeleteTempFiles());
    }
    private void CloseReporter(InputAction.CallbackContext context)
    {
        InputBridge.RestorePreviousContext();
        thankYouMessage.SetActive(false); 
    }
}