using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputBridge
{
    public enum InputContext
    {
        [InputContextConfig(mapName: nameof(IA_Global.Player), cursorVisible: false)]
        Player,

        [InputContextConfig(mapName: nameof(IA_Global.UI), cursorVisible: true)]
        UI,

        [InputContextConfig(mapName: nameof(IA_Global.DevConsole), cursorVisible: true)]
        DevConsole 
    }

    struct MapCursorPair
    {
        public InputActionMap map { get; private set; }
        public bool cursorVisible { get; private set; }

        public MapCursorPair(InputActionMap map, bool cursorVisible)
        {
            this.map = map;
            this.cursorVisible = cursorVisible;
        }
    }

    public static InputContext CurrentContext { get; private set; } = InputContext.Player;
    static InputContext previousContext = InputContext.Player;

    public static event Action<InputContext> OnContextChanged;
    static readonly Dictionary<InputContext, MapCursorPair> contextMap;

    public static IA_Global Actions { get; private set; }

    static InputBridge()
    {
        Actions = new IA_Global();
        contextMap = BuildContextMap();

        Actions.Global.SetCallbacks(new GlobalInputCallbacks());
        Actions.Global.Enable();
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

        foreach (var map in Actions.asset.actionMaps)
        {
            if (map == Actions.Global.Get()) continue;
            map.Disable();
        }

        var pair = contextMap[context];

        pair.map.Enable();
        SetCursor(pair.cursorVisible);
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
    static Dictionary<InputContext, MapCursorPair> BuildContextMap()
    {
        var map = new Dictionary<InputContext, MapCursorPair>();

        foreach (InputContext context in Enum.GetValues(typeof(InputContext)))
        {
            FieldInfo field = typeof(InputContext).GetField(context.ToString());
            var attribute = field.GetCustomAttribute<InputContextConfigAttribute>();

            if (attribute == null) throw new Exception($"[InputBridge] InputContext.{context} is missing an InputContextConfig attribute.");

            InputActionMap actionMap = Actions.asset.FindActionMap(attribute.MapName, throwIfNotFound: true);
            map[context] = new MapCursorPair(actionMap, attribute.CursorVisible);
        }

        return map;
    }
}