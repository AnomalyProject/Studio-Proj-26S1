using UnityEditor;

public static class SteamEditorToggle
{
    private const string MenuPath = "Dev/Steam/Enable Steam In Editor";

    [MenuItem(MenuPath)]
    private static void ToggleSteamInEditor()
    {
        SteamManager.SteamEnabledInEditor = !SteamManager.SteamEnabledInEditor;
        Menu.SetChecked(MenuPath, SteamManager.SteamEnabledInEditor);
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggleSteamInEditor()
    {
        Menu.SetChecked(MenuPath, SteamManager.SteamEnabledInEditor);
        return true;
    }
}