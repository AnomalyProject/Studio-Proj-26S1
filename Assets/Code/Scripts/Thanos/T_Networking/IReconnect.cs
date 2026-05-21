using System;

public interface IReconnect
{
    event Action OnConnectionLost;
    event Action OnHostMigrating;
    event Action OnReconnected;
    void CancelAndReturnToMenu();
}