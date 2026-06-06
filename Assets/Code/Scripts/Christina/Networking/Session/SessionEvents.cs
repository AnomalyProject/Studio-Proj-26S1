using System;

public static class SessionEvents
{

    public static event Action<ulong, string> OnPlayerJoined;
    public static event Action<ulong, string> OnPlayerLeft;
    public static event Action OnSessionDataChanged;
    public static event Action<SessionErrorResponse> OnSessionError;
    public static event Action<string> OnHostMigrationStarted;


    public static void InvokePlayerJoined(ulong steamID, string displayName)
    {
        OnPlayerJoined?.Invoke(steamID, displayName);
    }

    public static void InvokePlayerLeft(ulong steamID, string reason)
    {
        OnPlayerLeft?.Invoke(steamID, reason);
    }

    public static void InvokeSessionDataChanged()
    {
        OnSessionDataChanged?.Invoke();
    }

    public static void InvokeSessionError(SessionErrorResponse errorResponse)
    {
        OnSessionError?.Invoke(errorResponse);
    }

    public static void InvokeHostMigrationStarted(string newHostName)
    {
        OnHostMigrationStarted?.Invoke(newHostName);
    }

    public static void Reset()
    {
        OnPlayerJoined = null;
        OnPlayerLeft = null;
        OnSessionDataChanged = null;
        OnSessionError = null;
        OnHostMigrationStarted = null;
    }
}
