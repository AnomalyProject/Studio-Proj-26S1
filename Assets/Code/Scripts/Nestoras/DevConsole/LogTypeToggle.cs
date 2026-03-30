using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class LogTypeToggle : MonoBehaviour
{
    public LogType type;

    private static Dictionary<LogType, Sprite> g_icons = new Dictionary<LogType, Sprite>();
    private static Color depressedColor = Color.HSVToRGB(0, 0, 0.8f);
    private static Color liftedColor = Color.HSVToRGB(0, 0, 0.3f);
    private Image background;
    private Image icon;

    void Awake()
    {
        background = GetComponent<Image>();
        icon = transform.GetChild(0).GetComponent<Image>();

        Toggle(true);
    }

    public void Toggle(bool enabled)
    {
        if (enabled)
        {
            icon.sprite = DevConsole.icons[type];
            background.color = liftedColor;
        }
        else
        {
            icon.sprite = g_icons[type];
            background.color = depressedColor;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void FetchIcons()
    {
        g_icons.Add(LogType.Log, Resources.Load<Sprite>("DevConsole/g_log"));
        g_icons.Add(LogType.Warning, Resources.Load<Sprite>("DevConsole/g_warning"));
        Sprite error = Resources.Load<Sprite>("DevConsole/g_error");
        g_icons.Add(LogType.Error, error);
        g_icons.Add(LogType.Exception, error);
        g_icons.Add(LogType.Assert, error);
    }
}
