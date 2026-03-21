using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

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
    [SerializeField] private TextMeshProUGUI logText;

    [Header("Input")]
    private IA_DevConsole inputActions;
    private InputAction submitAction;
    private InputAction scrollAction;

    [Header("Settings")]
    [Tooltip("How many logs to keep in the console at once. -1 = Infinite")] [Min(-1)]
    [SerializeField] private int maxLogs = -1;
    [Tooltip("How many commands to keep in history. -1 = Infinite")]
    [Min(-1)]
    [SerializeField] private int historyDepth = 50;

    [Header("Events")]
    private List<string> logs = new List<string>();
    private List<string> commandHistory = new List<string>();
    public static Action<string[]> onCommandEntered;

    private bool logCommands = true;
    private int historyIndex = -1;
    private bool isOpen = false;

    #region Attach / Detach
    private void Awake()
    {
        inputActions = new IA_DevConsole();
        submitAction = inputActions.DevConsole.Submit;
        scrollAction = inputActions.DevConsole.ScrollHistory;
    }
    private void OnEnable()
    {
        onCommandEntered += CheckForBuiltInCommands;
        Application.logMessageReceived += HandleLog;

        inputActions.UI.ToggleConsole.performed += OnToggleConsole;
        submitAction.performed += OnSubmit;
        scrollAction.performed += OnScrollHistory;

        inputActions.Enable();

        submitAction.Disable();
        scrollAction.Disable();
    }
    private void OnDisable()
    {
        onCommandEntered -= CheckForBuiltInCommands;
        Application.logMessageReceived -= HandleLog;

        inputActions.UI.ToggleConsole.performed -= OnToggleConsole;
        submitAction.performed -= OnSubmit;
        scrollAction.performed -= OnScrollHistory;

        inputActions.Disable();
    }
    #endregion
    
    private void OnToggleConsole(InputAction.CallbackContext context)
    {
        isOpen = !isOpen;
        consoleRoot.SetActive(isOpen);

        if (isOpen)
        {
            submitAction.Enable();
            scrollAction.Enable();
            commandLine.ActivateInputField();
        }
        else
        {
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

        if (logCommands && input[0] != '@') // Suppress echo optionally
        {
            logs.Add($"> {input}");
            RefreshUI();
        }

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

    private void RefreshUI()
    {
        // Clear first logs to avoid goint over the limit
        if (maxLogs >= 0 && logs.Count > maxLogs) logs.RemoveAt(0);

        logText.text = string.Join("\n", logs);

        // Auto-scroll to bottom if already near bottom
        if (logArea.verticalNormalizedPosition <= 0.05f) StartCoroutine(nameof(ScrollToBottomNextFrame));
    }
    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        logArea.verticalNormalizedPosition = 0f;
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        string color = type switch
        {
            LogType.Error => "red",
            LogType.Assert => "red",
            LogType.Exception => "red",
            LogType.Warning => "yellow",
            LogType.Log => "white",
            _ => "white"
        };
        logs.Add($"<color={color}>{condition}</color>");

        RefreshUI();
    }

    #region Internal Commands
    private void CheckForBuiltInCommands(string[] args)
    {
        if (args.Length == 0) return;

        // Ignore echo suppression character
        if (args[0][0] == '@') args[0] = args[0].TrimStart('@');

        switch (args[0].ToLower())
        {
            case "cls":
                logs.Clear();
                RefreshUI();
                break;

            case "echo":
                if (args.Length < 2)
                {
                    Debug.Log($"ECHO is {(logCommands ? "on" : "off")}.");
                    break;
                }
                switch (args[1].ToLower())
                {
                    case "on":
                        logCommands = true;
                        break;
                    case "off":
                        logCommands = false;
                        break;
                    default:
                        Debug.Log(args[1]);
                        break;
                }
                break;

            case "warn":
                if (args.Length > 1) Debug.LogWarning(args[1]);
                else Debug.LogError("No arguments provided for 'warn'.");
                break;

            case "alert":
                if (args.Length > 1) Debug.LogError(args[1]);
                else Debug.LogError("No arguments provided for 'alert'.");
                break;
        }
    }
    #endregion
}
