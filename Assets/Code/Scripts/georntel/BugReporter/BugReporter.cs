using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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
    public string senderEmail = "";
    public string senderPassword = "";
    public string smtpHost = "";
    public int smtpPort = 587;
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
        // Press F12 to toggle the Bug Reporter
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ToggleReporter();
        }
    }

    public void ToggleReporter()
    {
        bool isActive = !bugReporterPanel.activeSelf;
        bugReporterPanel.SetActive(isActive);
        
        // Auto hide the thank you message when the panel is closed
        if (!isActive)
        {
            thankYouMessage.SetActive(false);
        }
    }

    // Ensures the submit button is only clickable when there is text
    private void ValidateInput(string input)
    {
        submitButton.interactable = !string.IsNullOrWhiteSpace(input) && !isSending;
    }

    private void OnSubmitClicked()
    {
        // Spam-proof check
        if (isSending) return;
        
        isSending = true;
        submitButton.interactable = false; 

        SendEmailReport();
    }

    private void SendEmailReport()
    {
        try
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(senderEmail);
            mail.To.Add(receiverEmail);
            mail.Subject = "Bug Report ";
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            string os = SystemInfo.operatingSystem;
            string userDescription = descriptionInput.text;

            mail.Body = $"Date: {currentDate}\n" +
                        $"Time: {currentTime}\n" +
                        $"OS: {os}\n\n" +
                        $"Player Description:\n{userDescription}";

            // SMTP Client
            SmtpClient smtpServer = new SmtpClient(smtpHost);
            smtpServer.Port = smtpPort;
            smtpServer.Credentials = new NetworkCredential(senderEmail, senderPassword) as ICredentialsByHost;
            smtpServer.EnableSsl = true;
            
            ServicePointManager.ServerCertificateValidationCallback = 
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
                { return true; };

            smtpServer.Send(mail);

            //  Clear text and show Thank You message when successfully submit
            descriptionInput.text = "";
            thankYouMessage.SetActive(true);
            Debug.Log("Bug report sent successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send bug report: {e.Message}");
        }
        finally
        {
            isSending = false;
            //  Ensure the button stays disabled if the text was cleared
            ValidateInput(descriptionInput.text); 
        }
    }

    private void CloseReporter()
    {
        bugReporterPanel.SetActive(false);
        thankYouMessage.SetActive(false); 
    }
}