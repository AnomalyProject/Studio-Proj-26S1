using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using PurrNet;
using TMPro;

public class KeypadInteractable : NetworkBehaviour
{
    public const int maxDigits = 4; // max digits a password can generate
    public const int glyphCount = 16; // number of glyphs
    [SerializeField] private string forcedPassword;

    public struct PasswordData
    {
        public string digits;
        public byte[] glyphIndicies;
        public PasswordData(string digits, byte[] glyphIndicies)
        {
            this.digits = digits;
            this.glyphIndicies = glyphIndicies;
        }
    }

    [Header("Debug")]
    public bool debugMouseInput; // use OnMouseDown() to interact with buttons for test purposes
    private bool _initialized = false;

    [Header("Events")]
    public UnityEvent OnAccessGranted;
    public UnityEvent OnAccessDenied;
    public UnityEvent<PasswordData> OnPasswordGenerated;
    public UnityEvent<string> OnPasswordDigitsGenerated, OnInputChanged;

    private string requiredPassword;
    private SyncVar<string> currentInput = new SyncVar<string>(ownerAuth: false); // sync var to display changes on all clients

    // read-only access
    public string RequiredInput => requiredPassword;
    public string CurrentInput => currentInput.value;


    [Header("World & Feedback")]
    [SerializeField] private TextMeshPro statusText;
    [SerializeField] private TextMeshPro inputText;
    [SerializeField] private TextMeshPro glyphsText;
    [SerializeField] private KeypadButton[] myButtons;

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

        if (inputText != null && statusText != null && glyphsText != null) 
        { 
            inputText.text = statusText.text = glyphsText.text = ""; // texts clear
        }

        if(!asServer)
        currentInput.onChanged += OnInputChanged.Invoke;
    }

    private void OnValidate()
    {
        if (forcedPassword.Length > maxDigits) forcedPassword = forcedPassword.Substring(0, maxDigits);
    }
    public void GenerateNewPassword()
    {
        if (!isServer) return;
        char[] digits = new char[maxDigits]; // create a new array with the size of maxDigits

        // Populate list with all possible glyphs (0-15) for random selection
        List<byte> possibleGlyphs = new List<byte>(glyphCount);
        for (byte i = 0; i < glyphCount; i++) possibleGlyphs.Add(i);

        byte[] glyphIndecies = new byte[maxDigits];

        for (int i = 0; i < maxDigits; i++) // loops as many times as the max possible digits
        {
            digits[i] = (char)('0' + Random.Range(0, 10)); // picks a random digit and inserts it into the array index i

            byte selectedGlyph = possibleGlyphs[Random.Range(0, possibleGlyphs.Count)]; // randomly select a glyph index from the remaining possible glyphs
            glyphIndecies[i] = selectedGlyph; // assign the selected glyph index
            possibleGlyphs.Remove(selectedGlyph);
        }
        requiredPassword = string.IsNullOrEmpty(forcedPassword) ? forcedPassword : new string(digits); // store password or use pre-generated one
        NotifyPasswordGenerated(new PasswordData(requiredPassword, glyphIndecies));

        // Question: How do the players get clues as to which the correct digits are? 
        // do they just guess??
        // is it a future task for this script to generate clues too?

        // Answer: yes. done.
    }

    [ObserversRpc(bufferLast: true)] // fire event on all clients
    private void NotifyPasswordGenerated(PasswordData passwordData)
    {
        OnPasswordGenerated?.Invoke(passwordData);
        OnPasswordDigitsGenerated?.Invoke(requiredPassword);
        SetGlyphs(passwordData.glyphIndicies); // set the glyphs for the password
    }

    private void SetGlyphs(byte[] glyphIndecies)
    {
        if (glyphsText == null) return;

        string glyphsDisplay = string.Empty;
        for (int i = 0; i < glyphIndecies.Length; i++) glyphsDisplay += $"<sprite={glyphIndecies[i]}>";
        glyphsText.text = glyphsDisplay;
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
        if (glyphsText != null) glyphsText.text = "";
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
