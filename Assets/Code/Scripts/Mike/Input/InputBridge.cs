using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

public static class InputBridge
{
    public static event Action<InputContext> OnContextChanged;

    #region Enum & Data Container
    public enum InputContext // Assign InputContextConfig attribute on each new enum value
    {
        [InputContextConfig(mapName: nameof(IA_Global.Player), cursorVisible: false)]
        Player,

        [InputContextConfig(mapName: nameof(IA_Global.UI), cursorVisible: true)]
        UI,

        [InputContextConfig(mapName: nameof(IA_Global.Chat), cursorVisible: true)]
        Chat,

        [InputContextConfig(mapName: nameof(IA_Global.UI), cursorVisible: true)]
        BugReporter,

        [InputContextConfig(mapName: nameof(IA_Global.DevConsole), cursorVisible: true)]
        DevConsole,
    }
    private struct MapCursorPair
    {
        public InputActionMap map { get; private set; }
        public bool cursorVisible { get; private set; }

        public MapCursorPair(InputActionMap map, bool cursorVisible)
        {
            this.map = map;
            this.cursorVisible = cursorVisible;
        }
    }
    #endregion

    #region Fields & Properties
    public static IA_Global Actions { get; private set; }
    public static InputContext CurrentContext { get; private set; } = InputContext.Player;
    private static Stack<InputContext> contextStack = new Stack<InputContext>();
    private static Dictionary<InputContext, MapCursorPair> contextMap;
    public static bool isLocked { get; private set; } = false;
    #endregion

    #region Exposed Methods

    /// <summary>
    /// Change the active action map.
    /// </summary>
    /// <param name="context"></param>
    public static void SetContext(InputContext context)
    {
        if (isLocked)
        {
            Debug.LogWarning("Input context is currently locked and cannot change.");
            return;
        }

        if (contextStack.Count == 0 || contextStack.Peek() != context) contextStack.Push(context);
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
    public static void RestorePreviousContext()
    {
        if(contextStack.Count > 1) contextStack.Pop();
        SetContext(contextStack.Peek());
    }
    /// <summary>
    /// Toggle between the previous action map and the provided one.
    /// </summary>
    /// <param name="context"></param>
    public static void ToggleContext(InputContext context)
    {
        if (context == CurrentContext) RestorePreviousContext();
        else SetContext(context);
    }
    public static void LockAt(InputContext context)
    {
        isLocked = false;
        SetContext(context);
        isLocked = true;
    }
    public static void Unlock() => isLocked = false;

    #endregion

    #region Helpers
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        Actions = new IA_Global();
        contextMap = BuildContextMap();

        Actions.Global.SetCallbacks(new GlobalInputCallbacks());
        Actions.Global.Enable();
        SetContext(InputContext.UI);
    }
    private static void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
    private static Dictionary<InputContext, MapCursorPair> BuildContextMap()
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
    #endregion
}