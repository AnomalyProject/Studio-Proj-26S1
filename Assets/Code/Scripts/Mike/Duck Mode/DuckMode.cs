using PurrNet;
using System;

public static class DuckMode
{
    public static bool modeActive { get; private set; } = false;
    public static event Action<bool> OnModeToggled;

    [ServerRpc] public static void ToggleMode_ServerRpc()
    {
        bool active = !modeActive;
        InvokeOnModeToggled_Observers(active);
    }

    [ObserversRpc] private static void InvokeOnModeToggled_Observers(bool active)
    {
        modeActive = active;
        OnModeToggled?.Invoke(active);
    }
}