using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public static class MailService
{
    public static readonly string senderEmail = "saeathenssteambuilds@gmail.com";
    public static readonly string senderPassword = "begc acxl qvtq ltpw";
    public static readonly string smtpHost = "smtp.gmail.com";
    public static readonly int smtpPort = 587;

    /// <summary>
    /// Sends an email from the dedicated email address for builds
    /// </summary>
    /// <param name="mail">A <see cref="MailMessage"/> with the <see cref="MailMessage.To"/>, <see cref="MailMessage.Body"/> & <see cref="MailMessage.Subject"/> fields pre-populated</param>
    /// <param name="onSuccess">Callback for successfult post</param>
    /// <param name="onFailure">Callback for any errors</param>
    public static void SendEmail(MailMessage mail, Action onSuccess, Action<string> onFailure)
    {
        try
        {
            mail.From = new MailAddress(senderEmail);
            SmtpClient smtpServer = new SmtpClient(smtpHost);
            smtpServer.Port = smtpPort;
            smtpServer.Credentials = new NetworkCredential(senderEmail, senderPassword) as ICredentialsByHost;
            smtpServer.EnableSsl = true;

            ServicePointManager.ServerCertificateValidationCallback = 
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
                { return true; };

            smtpServer.Send(mail);
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            onFailure?.Invoke(e.Message);
        }
    }
}