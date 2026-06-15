using System;

public interface IReconnect
{
    event Action OnConnectionLost;
    event Action OnHostMigrating;
    event Action OnReconnected;
    event Action<string> OnReconnectFailed;
    
    float ReconnectTimeoutSeconds { get; }
    bool IsAfk { get; }

    void TriggerAfkReconnect(); //AFK reconnect should also set the UI to Active(true).
    void CancelAfkReconnect();
    void CancelAndReturnToMenu(); //Cancel and return to menu should also set the UI to Active(false).
}