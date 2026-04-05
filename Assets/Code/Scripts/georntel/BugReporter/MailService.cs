using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public static class MailService
{
    public static void SendEmail(string host, int port, string user, string pass, MailMessage mail, Action onSuccess, Action<string> onFailure)
    {
        try
        {
            SmtpClient smtpServer = new SmtpClient(host);
            smtpServer.Port = port;
            smtpServer.Credentials = new NetworkCredential(user, pass) as ICredentialsByHost;
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