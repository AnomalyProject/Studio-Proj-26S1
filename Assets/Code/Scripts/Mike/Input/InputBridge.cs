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

        Actions.Global.ToggleDev.started += _ => ToggleDevConsole();
        Actions.Global.ToggleUI.started += _ => ToggleUI();

        contextMap = new()
        {
            [InputContext.Player] = new MapCursorPair(Actions.Player.Get(), false),
            [InputContext.UI] = new MapCursorPair(Actions.UI.Get(), true),
            [InputContext.DevConsole] = new MapCursorPair(Actions.DevConsole.Get(), true)
        };

        SetContext(InputContext.Player);
    }

    /// <summary>
    /// Change the active action map.
    /// </summary>
    /// <param name="context"></param>
    public static void SetContext(InputContext context)
    {
        previousContext = CurrentContext;
        CurrentContext = context;

        foreach (var map in Actions.asset.actionMaps) map.Disable();

        var pair = contextMap[context];

        pair.map.Enable();
        SetCursor(pair.cursorVisibility);

        Actions.Global.Enable();
        OnContextChanged?.Invoke(context);
    }
    public static void RestorePreviousContext() => SetContext(previousContext);
    static void ToggleDevConsole()
    {
        if (CurrentContext == InputContext.DevConsole) RestorePreviousContext();
        else SetContext(InputContext.DevConsole);
    }
    static void ToggleUI()
    {
        if (CurrentContext == InputContext.DevConsole) return;

        if (CurrentContext == InputContext.UI) RestorePreviousContext();
        else SetContext(InputContext.UI);
    }
    static void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}