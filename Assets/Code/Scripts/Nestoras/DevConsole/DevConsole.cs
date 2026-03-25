using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Text;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

/// <summary>
/// Nestoras
/// 
/// A surface for logs and exceptions to appear inside the build.
/// Has some internal commands and allows other scripts to subscribe to them, or register their own
/// </summary>
public class DevConsole : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The root gameobject of the console. MUST NOT CONTAIN THIS SCRIPT. Used for toggling the console window.")]
    [SerializeField] private GameObject consoleRoot;
    [Tooltip("The TMP Input Field where the user writes commands")]
    [SerializeField] private TMP_InputField commandLine;
    [Tooltip("The scrollRect containing the log TextField")]
    [SerializeField] private ScrollRect logArea;
    [Tooltip("The TMP Text Field inside the \'Content\' gameobject of the logArea")]
    [SerializeField] private GameObject logPrefab;
    [SerializeField] private Transform content;

    [Header("Input")]
    private IA_DevConsole inputActions;
    private InputAction submitAction;
    private InputAction scrollAction;

    [Header("Settings")]
    [Tooltip("How many logs to keep in the console at once. -1 = Infinite")] [Min(-1)]
    [SerializeField] private int maxLogs = -1;
    [Tooltip("How many commands to keep in history. -1 = Infinite")] [Min(-1)]
    [SerializeField] private int historyDepth = 50;
    
    private static bool logCommands = true;
    private int historyIndex = -1;
    private bool isOpen = false;
    private List<string> commandHistory = new List<string>();

    [Header("Events")]
    public static Action<string[]> onCommandEntered;
    public UnityEvent onConsoleToggledOn;
    public UnityEvent onConsoleToggledOff;
    public UnityEvent<LogEntry> onLogReceived;

    [Header("Structures")]
    private static readonly List<DevConsole> devConsoles = new List<DevConsole>();
    private readonly Queue<GameObject> logObjects = new();
    private readonly Queue<LogEntry> logs = new();
    [Serializable]
    public class LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        
        public DateTime timestamp;
        public TextMeshProUGUI textComponent;
        public bool showStackTrace;

        public LogEntry(string message = null, string stackTrace = null, LogType type = default)
        {
            this.message = message;
            this.stackTrace = stackTrace;
            this.type = type;

            timestamp = DateTime.Now;
        }

        public string Format(bool includeStackTrace = true, bool includeTimestamp = false)
        {
            StringBuilder sb = new StringBuilder();
            if (includeTimestamp) sb.Append($"[{timestamp}] ");
            sb.Append($"[{type}] {message}");

            if (includeStackTrace && !string.IsNullOrWhiteSpace(stackTrace))
            {
                sb.AppendLine();
                sb.Append(stackTrace.TrimEnd('\n'));
            }
            return sb.ToString();
        }
    }

    public static Dictionary<string, CommandData> commands { get; } = new Dictionary<string, CommandData>();
    [Serializable]
    public class CommandData
    {
        public string description { get; }
        public Action<string[]> callback;
        public string manual { get; }

        /// <param name="description">Appears in help list</param>
        /// <param name="callback">Called when executing the command from the console</param>
        /// <param name="manual">Extra info about the command. Appears when executing "help <commandName>"</param>
        public CommandData(string description, Action<string[]> callback, string manual = null)
        {
            this.description = description;
            this.callback = callback;
            this.manual = manual;
        }

        public void Execute(string[] args) => callback?.Invoke(args);
    }

    [Header("Multi-threaded logging")]
    private static int mainThreadId;
    private static readonly Queue<LogEntry> mainThreadLogQueue = new();
    private static readonly object queueLock = new();
    private static class UnityMainThreadDispatcher
    {
        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;
    }

    #region Attach / Detach
    private void Awake()
    {
        mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        commands["cls"].callback += ClearScreen;

        inputActions = new IA_DevConsole();
        submitAction = inputActions.DevConsole.Submit;
        scrollAction = inputActions.DevConsole.ScrollHistory;
    }
    private void OnEnable()
    {
        devConsoles.Add(this);

        onCommandEntered += CheckForBuiltInCommands;

        inputActions.UI.ToggleConsole.performed += OnToggleConsole;
        submitAction.performed += OnSubmit;
        scrollAction.performed += OnScrollHistory;

        inputActions.Enable();

        submitAction.Disable();
        scrollAction.Disable();
    }
    private void OnDisable()
    {
        devConsoles.Remove(this);

        onCommandEntered -= CheckForBuiltInCommands;

        inputActions.UI.ToggleConsole.performed -= OnToggleConsole;
        submitAction.performed -= OnSubmit;
        scrollAction.performed -= OnScrollHistory;

        inputActions.Disable();
    }
    #endregion

    #region Navigation
    private void OnToggleConsole(InputAction.CallbackContext context)
    {
        isOpen = !isOpen;
        consoleRoot.SetActive(isOpen);

        if (isOpen)
        {
            onConsoleToggledOn?.Invoke();
            submitAction.Enable();
            scrollAction.Enable();
            commandLine.ActivateInputField();
        }
        else
        {
            onConsoleToggledOff?.Invoke();
            submitAction.Disable();
            scrollAction.Disable();
        }
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        string input = commandLine.text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        commandHistory.Add(input);
        // Clear first commands to avoid going over the limit
        if (historyDepth >= 0 && commandHistory.Count -1 >= historyDepth) commandHistory.RemoveAt(0);
        historyIndex = commandHistory.Count;

        if (logCommands && input[0] != '@') Log(new LogEntry($"> {input}", "", LogType.Log), false); // Suppress echo optionally

        commandLine.text = "";
        commandLine.ActivateInputField();

        string[] args = ParseArguments(input);
        onCommandEntered?.Invoke(args);        
    }
    private string[] ParseArguments(string input)
    {
        List<string> args = new List<string>();

        bool inQuotes = false;
        System.Text.StringBuilder current = new System.Text.StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == '\\' && i + 1 < input.Length && input[i + 1] == '"')
            {
                current.Append('"');
                i++; // skip next char
                continue;
            }

            // Split on spaces ONLY if not inside quotes
            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else current.Append(c);
        }

        // Add last argument
        if (current.Length > 0) args.Add(current.ToString());

        return args.ToArray();
    }

    private void OnScrollHistory(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();

        if (Mathf.Approximately(value, 0)) return;

        if (commandHistory.Count == 0) return;

        historyIndex += value > 0 ? -1 : 1;
        historyIndex = Mathf.Clamp(historyIndex, 0, commandHistory.Count);

        commandLine.SetTextWithoutNotify(historyIndex == commandHistory.Count ? "" : commandHistory[historyIndex]);
        StartCoroutine(nameof(SetCaretToEndNextFrame));
    }
    private IEnumerator SetCaretToEndNextFrame()
    {
        yield return null;

        commandLine.ActivateInputField();
        commandLine.ForceLabelUpdate();

        int pos = commandLine.text.Length;
        commandLine.caretPosition = pos;
        commandLine.selectionAnchorPosition = pos;
        commandLine.selectionFocusPosition = pos;
    }
    #endregion

    #region Logging
    public static void Propagate(LogEntry entry, bool includeStackTrace = true)
    {
        if (devConsoles.Count == 0) return;

        // Log instantly if on main thread, otherwise enqueue for next update
        if (UnityMainThreadDispatcher.IsMainThread) foreach (var console in devConsoles) console.Log(entry, includeStackTrace);
        else lock (queueLock) mainThreadLogQueue.Enqueue(entry);
    }
    private void Update()
    {
        // Process any queued log entries from other threads
        while (true)
        {
            LogEntry entry = null;
            lock (queueLock)
            {
                if (mainThreadLogQueue.Count > 0) entry = mainThreadLogQueue.Dequeue();
                else break;
            }

            if (entry != null) Log(entry);
        }
    }
    private void Log(LogEntry entry, bool includeStackTrace = true)
    {
        // Enforce max logs and reuse oldest log GameObject
        GameObject log = null;
        if (maxLogs >= 0 && logs.Count >= maxLogs)
        {
            logs.Dequeue();
            log = logObjects.Dequeue();
            log.transform.SetAsLastSibling();
        }
        else log = Instantiate(logPrefab, content);

        entry.textComponent = log.GetComponent<TextMeshProUGUI>();
        AdvancedButton button = log.GetComponent<AdvancedButton>();
        UpdateLogEntryUIText(entry); // Populate text

        // Copy on right click
        button.OnRightClick.AddListener(() => GUIUtility.systemCopyBuffer = entry.Format(true, true));
        // Add stack trace toggle on left click
        if (includeStackTrace)
        {
            button.OnLeftClick.AddListener(() =>
            {
                entry.showStackTrace = !entry.showStackTrace;
                UpdateLogEntryUIText(entry);
            });
        }

        logs.Enqueue(entry);
        logObjects.Enqueue(log);

        // Scroll to bottom if already near the bottom
        if (logArea.verticalNormalizedPosition <= 0.05f) StartCoroutine(nameof(ScrollToBottomNextFrame));

        onLogReceived?.Invoke(entry);
    }
    private void UpdateLogEntryUIText(LogEntry entry)
    {
        string color = entry.type switch
        {
            LogType.Error => "red",
            LogType.Assert => "red",
            LogType.Exception => "red",
            LogType.Warning => "yellow",
            _ => "white"
        };

        entry.textComponent.text = $"<line-indent=0.5em><color={color}>{entry.message}</color></line-indent>";
        if (entry.showStackTrace) entry.textComponent.text += $"<line-indent=0.5em><color={color}><size=0.8em><indent=1.2em>\n{entry.stackTrace}</indent></size></color></line-indent>";
    }
    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        logArea.verticalNormalizedPosition = 0f;
    }
    #endregion

    #region Commands
    // Register some basic internal commands
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InternalCommands()
    {
        RegisterCommand("cls", new CommandData("Clears the screen.", args => { }));
        RegisterCommand("exit", new CommandData("Closes the game.", args =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }, "Will also exit playmode in the editor."));
        RegisterCommand("help", new CommandData("Shows this list.", args =>
        {
            StringBuilder sb = new StringBuilder();

            // Show all commands
            if (args.Length < 1)
            {
                sb.AppendLine("<b>Available Commands:</b>");
                foreach (KeyValuePair<string, CommandData> entry in commands.OrderBy(c => c.Key)) sb.AppendLine($"<color=#00FFFF>{entry.Key}</color><pos=10em>: {entry.Value.description}");

                Debug.Log(sb.ToString());
                return;
            }
            if (TryGetCommand(args[0], out CommandData command))
            {
                sb.AppendLine($"<b><color=#00FFFF>{args[0].ToLower()}</color></b>");
                sb.AppendLine(command.description);
                if (!string.IsNullOrEmpty(command.manual)) sb.AppendLine(command.manual);

                Debug.Log(sb.ToString());
            }
            else Debug.LogWarning($"Unknown command: {args[0]}");
        }, "You may pass a registered command as an argument to show more info about it."));
        RegisterCommand("echo", new CommandData("Log a string.", args =>
        {
            if (args.Length < 1)
            {
                Debug.Log($"ECHO is {(logCommands ? "on" : "off")}.");
                return;
            }
            switch (args[0].ToLower())
            {
                case "on":
                    logCommands = true;
                    break;
                case "off":
                    logCommands = false;
                    break;
                default:
                    Debug.Log(args[0]);
                    break;
            }
        }, "Passing <color=green>on</color>/<color=green>off</color> as arguments toggles automatic command logging. You may use '@' before your command to suppress the echo."));
        RegisterCommand("exception", new CommandData("Causes an exception.", args =>
        {
            switch (args[0])
            {
                case "null":
                    object obj = null;
                    obj.ToString();
                    break;

                case "divide0":
                    int x = 0;
                    int y = 10 / x;
                    break;

                case "tasksetup":
                    Task.Run(() => throw new Exception("Task exception!"));
                    break;

                case "taskflush":
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    break;

                default:
                    throw new Exception("Test exception (Unity handled)");
            }
        }, "Arguments: <color=green>null</color> causes a null reference exception.\n<color=green>divide0</color> causes a devide by zero exception.\n<color=green>tasksetup</color> throws an unobserved exception on a new thread.\nUse <color=green>taskflush</color> to force garbage collection."));
    }
    private void ClearScreen(string[] args)
    {
        while (logObjects.Count > 0) Destroy(logObjects.Dequeue());
    }

    // Command API
    public static void RegisterCommand(string name, CommandData commandData) => commands.TryAdd(name, commandData);
    public static bool TryGetCommand(string name, out CommandData commandData) => commands.TryGetValue(name.ToLower(), out commandData);
    private void CheckForBuiltInCommands(string[] args)
    {
        if (args.Length == 0) return;

        string commandName = args[0].TrimStart('@').ToLower();
        string[] commandArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

        if (TryGetCommand(commandName, out CommandData commandData)) commandData.Execute(commandArgs);
        else Debug.LogWarning($"Unknown command: {commandName}");
    }
    #endregion
}
