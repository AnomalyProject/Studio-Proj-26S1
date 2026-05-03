using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputBridge
{
    public enum InputContext  // Dont forget to assign new values in the dictionary inside the constructor.
    { 
        Player, 
        UI, 
        DevConsole 
    }

    struct MapCursorPair
    {
        public InputActionMap map { get; private set; }
        public bool cursorVisibility { get; private set; }

        public MapCursorPair(InputActionMap map, bool cursorVisibility)
        {
            this.map = map;
            this.cursorVisibility = cursorVisibility;
        }
    }

    public static InputContext CurrentContext { get; private set; } = InputContext.Player;
    static InputContext previousContext = InputContext.Player;

    public static event Action<InputContext> OnContextChanged;
    static readonly Dictionary<InputContext, MapCursorPair> contextMap;

    public static IA_Global Actions { get; private set; }
    public static IA_Global.PlayerActions Player => Actions.Player;
    public static IA_Global.UIActions UI => Actions.UI;
    public static IA_Global.DevConsoleActions DevConsole => Actions.DevConsole;

    static InputBridge()
    {
        Actions = new IA_Global();

        Actions.Global.ToggleDev.started += _ => ToggleContext(InputContext.DevConsole);
        Actions.Global.ToggleUI.started += _ => ToggleContext(InputContext.UI);

        contextMap = new() // Assign any new enum-map value here.
        {
            [InputContext.Player] = new MapCursorPair(Actions.Player.Get(), false),
            [InputContext.UI] = new MapCursorPair(Actions.UI.Get(), true),
            [InputContext.DevConsole] = new MapCursorPair(Actions.DevConsole.Get(), true)
        };

        SetContext(InputContext.Player);
        Actions.Global.Enable();
    }

    /// <summary>
    /// Change the active action map.
    /// </summary>
    /// <param name="context"></param>
    public static void SetContext(InputContext context)
    {
        previousContext = CurrentContext;
        CurrentContext = context;

        foreach (var map in Actions.asset.actionMaps)
        {
            if (map == Actions.Global.Get()) continue;
            map.Disable();
        }

        var pair = contextMap[context];

        pair.map.Enable();
        SetCursor(pair.cursorVisibility);
        OnContextChanged?.Invoke(context);
    }

    /// <summary>
    /// Restore the previously active action map.
    /// </summary>
    public static void RestorePreviousContext() => SetContext(previousContext);

    /// <summary>
    /// Toggle between the previous action map and the provided one.
    /// </summary>
    /// <param name="context"></param>
    public static void ToggleContext(InputContext context)
    {
        if (context == CurrentContext) RestorePreviousContext();
        else SetContext(context);
    }
    static void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}