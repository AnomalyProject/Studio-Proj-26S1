using System;

public static class SessionEvents
{

    public static event Action<ulong, string> OnPlayerJoined;
    public static event Action<ulong, string> OnPlayerLeft;
    public static event Action OnSessionDataChanged;
    public static event Action<SessionErrorResponse> OnSessionError;
    public static event Action<string> OnHostMigrationStarted;
    public static event Action OnReconnectApproved;
    public static event Action OnLocalSessionReady;
    public static event Action<ClientKickVoteData> OnKickVoteUpdated;
    public static event Action<bool, string> OnKickVoteFinished;
    public static event Action<string> OnLocalPlayerKicked;
    


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
    
    public static void InvokeReconnectApproved()
    {
        OnReconnectApproved?.Invoke();
    }
    
    public static void InvokeLocalSessionReady()
    {
        OnLocalSessionReady?.Invoke();
    }
    
    public static void InvokeKickVoteUpdated(ClientKickVoteData data)
    {
        OnKickVoteUpdated?.Invoke(data);
    }

    public static void InvokeKickVoteFinished(bool succeeded, string message)
    {
        OnKickVoteFinished?.Invoke(succeeded, message);
    }

    public static void InvokeLocalPlayerKicked(string message)
    {
        OnLocalPlayerKicked?.Invoke(message);
    }

    public static void Reset()
    {
        OnPlayerJoined = null;
        OnPlayerLeft = null;
        OnSessionDataChanged = null;
        OnSessionError = null;
        OnHostMigrationStarted = null;
        OnReconnectApproved = null;
        OnLocalSessionReady = null;
        OnKickVoteUpdated = null;
        OnKickVoteFinished = null;
        OnLocalPlayerKicked = null;
    }
}
