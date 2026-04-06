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
    public static readonly string logFilePath = Path.Combine(Application.persistentDataPath, "Console.log");
    public static readonly string oldLogFilePath = Path.Combine(Application.persistentDataPath, "Console-prev.log");
    private static readonly object fileLock = new();
    private static bool initialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Init()
    {
        if (initialized) return;
        initialized = true;

        RotateLogs();

        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleTaskException;
        Application.logMessageReceivedThreaded += HandleUnityLog;
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

    private static void RotateLogs()
    {
        try
        {
            lock (fileLock)
            {
                // Delete previous backup if it exists
                if (File.Exists(oldLogFilePath)) File.Delete(oldLogFilePath);

                // Move current log to -prev
                if (File.Exists(logFilePath)) File.Move(logFilePath, oldLogFilePath);
            }
        }
        catch { }
    }
}