using System;

public interface IReconnectService
{
    event Action OnConnectionLost;
    event Action OnHostMigrating;
    event Action OnReconnected;
    void CancelAndReturnToMenu();
}