using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class KeypadInteractable : NetworkBehaviour
{
    public const int maxDigits = 4; // max digits a password can generate

    [Header("Debug")]
    public bool debugMouseInput; // use OnMouseDown() to interact with buttons for test purposes
    private bool _initialized = false;

    [Header("Events")]
    public UnityEvent OnAccessGranted;
    public UnityEvent OnAccessDenied;
    public UnityEvent<string> OnPasswordGenerated, OnInputChanged;

    private string requiredPassword;
    private SyncVar<string> currentInput = new SyncVar<string>(ownerAuth: false); // sync var to display changes on all clients

    // read-only access
    public string RequiredInput => requiredPassword;
    public string CurrentInput => currentInput.value;


    [Header("World & Feedback")]
    [SerializeField] TextMeshPro inputText;
    [SerializeField] TextMeshPro statusText;
    [SerializeField] KeypadButton[] myButtons;

    private void OnEnable()
    {
        if (!_initialized) return; // only fires from OnSpawned onwards
        GenerateNewPassword(); // requirement
    }

    protected override void OnSpawned(bool asServer)
    {
        GenerateNewPassword();  // essential for isServer to be true
        _initialized = true;
        InitializeButtons();

        if(asServer) currentInput.value = "";

        if (inputText != null && statusText != null) 
        { 
            inputText.text = statusText.text = ""; // texts clear
        }

        if(!asServer)
        currentInput.onChanged += OnInputChanged.Invoke;
    }

    public void GenerateNewPassword()
    {
        if (!isServer) return;
        char[] digits = new char[maxDigits]; // create a new array with the size of maxDigits

        for (int i = 0; i < maxDigits; i++) // loops as many times as the max possible digits
        {
            digits[i] = (char)('0' + Random.Range(0, 10)); // picks a random digit and inserts it into the array index i
        }
        requiredPassword = new string(digits); // store password
        NotifyPasswordGenerated(requiredPassword);

        // Question: How do the players get clues as to which the correct digits are? 
        // do they just guess??
        // is it a future task for this script to generate clues too?
    }

    [ObserversRpc(bufferLast: true)]
    private void NotifyPasswordGenerated(string password)     // fire event on all clients
    {
        Debug.Log($"Password generated RPC received: {password}");
        OnPasswordGenerated?.Invoke(password);
    }

    void InitializeButtons()
    {
        if (myButtons != null && myButtons.Length > 0)
        {
            for (int i = 0; i < myButtons.Length; i++)
            {
                myButtons[i].myKeypad = this; // assign this reference to every button
            }
        }
    }


    [ServerRpc(requireOwnership: false)]
    public void PressDigit(int digit)
    {
        if (digit == -1)  //clear
        {
            currentInput.value = "";
            NotifyClear();
            return;
        }

        if (digit < 0 || digit > 9) return; // prevent invalid input

        // Ignore input if we are already at max length waiting for evaluation
        if (currentInput.value.Length >= maxDigits) return;

        currentInput.value += digit.ToString();
        NotifyInputChange(currentInput.value);

        if (currentInput.value.Length == maxDigits)
        {
            EvaluateInput();
        }
    }

    // Check if correct code
    private void EvaluateInput()
    {
        if (currentInput.value == requiredPassword)
        {
            NotifyAccessGranted();
        }
        else
        {
            currentInput.value = "";
            NotifyAccessDenied();
        }
    }




    // Status RPCs
    [ObserversRpc(bufferLast: true)]
    private void NotifyAccessGranted()
    {
        if (statusText != null)
        {
            statusText.color = Color.green;
            statusText.text = "Access Granted";
        }
        if (inputText != null) inputText.text = "";
        OnAccessGranted?.Invoke();
    }


    [ObserversRpc(bufferLast: true)]
    private void NotifyAccessDenied()
    {
        if (statusText != null)
        {
            statusText.color = Color.red;
            statusText.text = "Access Denied";
        }
        if (inputText != null) inputText.text = "";
        OnAccessDenied?.Invoke();
    }


    [ObserversRpc(bufferLast: true)]
    private void NotifyClear()
    {
        if (statusText != null)
        {
            statusText.color = Color.orange;
            statusText.text = "Input Cleared"; 
        }
        if (inputText != null) inputText.text = "";
    }


    [ObserversRpc(bufferLast: true)]
    private void NotifyInputChange(string newValue)
    {
        if (inputText && statusText != null)
        {
            inputText.text = newValue; // show input text
            statusText.text = ""; // no status visible when input text is on screen     
        }
    }
}
