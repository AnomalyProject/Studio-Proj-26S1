using System;

public interface IAfk
{
    event Action OnAfkDetected;

    event Action OnAfkCancelled;
    bool IsAfk { get; }
}