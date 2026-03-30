using System.Threading.Tasks;
using System.IO;
using System;
using static DevConsole;
using UnityEngine;

/// <summary>
/// Nestoras
/// 
/// Logs debug messages and exceptions from any thread to the developer console and a log file.
/// </summary>
public static class ExceptionLogger
{
    private static readonly string logFilePath = Path.Combine(Application.persistentDataPath, "console.log");
    private static readonly object fileLock = new();
    private static bool initialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Init()
    {
        if (initialized) return;
        initialized = true;

        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleTaskException;
        Application.logMessageReceivedThreaded += HandleUnityLog;

        WriteToFile(new LogEntry("=============== NEW SESSION ===============", "", LogType.Log).Format(false, true));
    }

    private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Attempt to convert it to an exception
        Exception ex = e.ExceptionObject as Exception;
        LogEntry entry;
        if (ex != null) entry = new LogEntry($"UNHANDLED: {ex.Message}", ex.StackTrace, LogType.Exception);
        else entry = new LogEntry($"UNHANDLED: {e}", "", LogType.Exception);

        WriteToFile(entry.Format(true, true));
        Propagate(entry);
    }

    private static void HandleTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        foreach (var ex in e.Exception.Flatten().InnerExceptions)
        {
            LogEntry entry = new LogEntry($"TASK EXCEPTION: {ex.Message}", ex.StackTrace, LogType.Exception);
            WriteToFile(entry.Format(true, true));
            Propagate(entry);
        }
        e.SetObserved();
    }

    private static void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        LogEntry entry = new LogEntry(condition, stackTrace, type);
        // if (type != LogType.Log) // Use this to skip Debug.Log events in the log file
        WriteToFile(entry.Format(includeStackTrace: true, includeTimestamp: true));
        Propagate(entry);
    }

    public static void WriteToFile(string text)
    {
        try { lock(fileLock) File.AppendAllText(logFilePath, $"{text}\n\n"); }
        catch { }
    }
}