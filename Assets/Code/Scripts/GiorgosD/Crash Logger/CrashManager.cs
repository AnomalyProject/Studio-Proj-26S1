using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using UnityEngine;
using UnityEngine.Diagnostics;

public class CrashManager : MonoBehaviour
{
    private static string flagPath;
    private static string crashZip;
    private static int pid;
    private static string watchdogPath;

    /// <summary>
    /// Initializes application-level resources and prepares the environment before the first scene is loaded.
    /// This ensures that the crash manager is set up and the watchdog process is launched as early as possible.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static private void Initialize()
    {
        flagPath = Path.Combine(Application.persistentDataPath, "exit_success.tmp");

        crashZip = Path.Combine(Application.persistentDataPath, "CrashReport.zip");

        if (File.Exists(flagPath)) File.Delete(flagPath);

        if (File.Exists(crashZip))
        {
            EmailCrashReport();
        }
        else
        {
            UnityEngine.Debug.Log("[CrashManager]: Failed to find Report.");
        }

        CreateObject();

        LaunchWatchdog();
    }

    /// <summary>
    /// Sends an Email when the game opens if it detects a zipped crash report
    /// </summary>
    static private void EmailCrashReport()
    {
        MailMessage mail = new MailMessage();
        mail.To.Add("");       //add mail for test
        mail.Subject = $"Crash Report: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        mail.Body = "A crash was detected. Please see the attached ZIP for logs and memory dumps.";

        try
        {
            mail.Attachments.Add(new Attachment(crashZip));

            MailService.SendEmail(
                mail,
                onSuccess: () => {
                    UnityEngine.Debug.Log("[CrashManager]: Email sent successfully.");

                    if (File.Exists(crashZip)) File.Delete(crashZip);
                },
                onFailure: (error) => {
                    UnityEngine.Debug.LogError($"[CrashManager]: Email failed to send: {error}");
                }
                );
        }
        catch(Exception exeption)
        {
            UnityEngine.Debug.LogError($"[CrashManager]: Email failed to send: {exeption}");
        }
    }

    /// <summary>
    /// Creates a new GameObject for the CrashManager component and ensures it persists across scene loads.
    /// Needed for unity to notify the OnApplicationQuit event.
    /// </summary>
    static private void CreateObject()
    {
        GameObject obj = new GameObject("CrashManager");
        obj.AddComponent<CrashManager>();
        DontDestroyOnLoad(obj);
        obj.hideFlags = HideFlags.HideInHierarchy;
    }

    /// <summary>
    /// THIS SHOULD BE DELETED AFTER TESTING!!! 
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            UnityEngine.Debug.Log("[CrashManager]: Crashed Game!!!!");

            Utils.ForceCrash(ForcedCrashCategory.FatalError);
        }
    }

    /// <summary>
    /// Launches the watchdog process, passing the current process ID and the path to the exit flag file.
    /// </summary>
    static private void LaunchWatchdog()
    {
        pid = Process.GetCurrentProcess().Id;

        watchdogPath = Path.Combine(Application.streamingAssetsPath, "Watchdog.exe");

        if (!File.Exists(watchdogPath))
        {
            UnityEngine.Debug.LogWarning("[CrashManager]: Watchdog binary missing. Crash reporting disabled.");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = watchdogPath,
            Arguments = $"{pid} \"{flagPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            Process.Start(startInfo);
            UnityEngine.Debug.Log("[CrashManager]: Crash Logger started successfully");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[CrashManager]: Leash failed to attach ({e.Message})");
        }
    }

    /// <summary>
    /// Handles application quit events by writing a clean exit flag to the specified file. 
    /// And ensures that the file is flushed to disk immediately to prevent false crash reports on next launch.
    /// </summary>
    private void OnApplicationQuit()
    {
        try
        {
            string timestamp = $"[CrashManager]: Clean Exit at {System.DateTime.Now.ToString()}";

            using (FileStream file = new FileStream(flagPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] info = new System.Text.UTF8Encoding(true).GetBytes(timestamp);
                file.Write(info, 0, info.Length);

                file.Flush(true);
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[CrashManager]: Failed to write exit flag ({e.Message})");
        }
    }
}
