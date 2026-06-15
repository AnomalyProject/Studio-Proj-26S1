using System;

public interface IReconnect
{
    event Action OnConnectionLost;
    event Action OnHostMigrating;
    event Action OnReconnected;
    
    float ReconnectTimeoutSeconds { get; }
    bool IsAfk { get; }

    void TriggerAfkReconnect();
    void CancelAfkReconnect();
    void CancelAndReturnToMenu(); //Cancel and return to menu should also set the UI to Active(false).
}