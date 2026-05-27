using UnityEngine;
using UnityEngine.InputSystem;

public class NoClip : MonoBehaviour
{
    #region NoClip
    #region Veriables
    private static GameObject freeCam;
    private static GameObject localPlayer;
    private static Camera camCache;
    private static bool isNoClipActive;
    #endregion
    
    #region Asign NoClip
    /// <summary>
    /// Creates The noClip cheat by being called in Mikes InGameCheats script
    /// </summary>
    public static void CreateNoClip()
    {
        DevConsole.CommandData noClipComm = new DevConsole.CommandData("Enables/Disables the ability to fly out of your body and pass through walls.", NoClipCheat);
        DevConsole.RegisterCommand("freecam", noClipComm);
    }
    #endregion

    #region Toggle NoClip
    /// <summary>
    /// Activation Function
    /// </summary>
    /// <param name="args"></param>
    private static void NoClipCheat(string[] args)
    {
        if (!isNoClipActive)
        {
            EnableNoClip();
        }
        else
        {
            DisableNoClip();
        }
    }
    #endregion
    
    #region Enabled NoClip
    /// <summary>
    /// Enables noClip
    /// </summary>
    private static void EnableNoClip()
    {
        isNoClipActive = true;
        
        foreach (var player in GameObject.FindObjectsByType<FPSController>(FindObjectsSortMode.InstanceID))
        {
            if (player.IsLocalPlayer)
            {
                localPlayer = player.gameObject;
                break;
            }
        }
        
        InputBridge.SetContext(InputBridge.InputContext.noClip);
        
        foreach (var cam in localPlayer.GetComponentsInChildren<Camera>())
        {
            cam.enabled = false;
        }
        
        CreateFreeCamObj();
    }
    #endregion
    
    #region Disabled NoClip
    /// <summary>
    /// Disables noClip
    /// </summary>
    private static void DisableNoClip()
    {
        Destroy(freeCam);
        
        foreach (var cam in localPlayer.GetComponentsInChildren<Camera>())
        {
            cam.enabled = true;
        }

        isNoClipActive = false;
        
        InputBridge.SetContext(InputBridge.InputContext.Player);
    }
    #endregion

    #region Create NoClip Object
    /// <summary>
    /// Creates noClip OBJ
    /// </summary>
    private static void CreateFreeCamObj()
    {
        freeCam = new GameObject("FreeCam");
        
        freeCam.AddComponent<Camera>();
        freeCam.AddComponent<NoClip>();
        freeCam.transform.position = GameObject.Find("Main Camera").transform.position;
        freeCam.transform.rotation = GameObject.Find("Main Camera").transform.rotation;
        
    }
    #endregion

    #region HandleContext
    /// <summary>
    /// Changes Player input back to No Clip if its still active
    /// </summary>
    /// <param name="context"></param>
    private static void HandleContext(InputBridge.InputContext context)
    {
        if (!isNoClipActive) return;
        
        if (context == InputBridge.InputContext.Player)
        {
            Debug.Log("Im Back bitch");
            InputBridge.SetContext(InputBridge.InputContext.noClip);
        }
    }
    #endregion
    #endregion
    
    #region NoClip Controller
    #region Variables
    // Move Stuff
    [SerializeField] private float runSpeed = 30.0f;
    [SerializeField] private float moveSpeed = 10.0f;
    private float speed;
    private bool isRunning;
    
    // Input values
    private Vector2 moveInput;
    private Vector2 lookInput;
    
    // Look stuff
    private float rotX;
    private float rotY;
    #endregion
    
    #region Subscribe/Unsubscribe To Input Events
    private void Awake()
    {
        InputBridge.OnContextChanged += HandleContext;
        
        InputBridge.Actions.NoClip.Move.performed += MoveValue;
        InputBridge.Actions.NoClip.Move.canceled += MoveValue;
        
        InputBridge.Actions.NoClip.Look.performed += LookValue;
        InputBridge.Actions.NoClip.Look.canceled += LookValue;
        
        InputBridge.Actions.NoClip.Sprint.performed += StartRunning;
        InputBridge.Actions.NoClip.Sprint.canceled += StopRunning;
    }

    private void OnDestroy()
    {
        isNoClipActive = false;
        
        InputBridge.OnContextChanged -= HandleContext;
        
        InputBridge.Actions.NoClip.Move.performed -= MoveValue;
        InputBridge.Actions.NoClip.Move.canceled -= MoveValue;
        
        InputBridge.Actions.NoClip.Look.performed -= LookValue;
        InputBridge.Actions.NoClip.Look.canceled -= LookValue;
        
        InputBridge.Actions.NoClip.Sprint.performed -= StartRunning;
        InputBridge.Actions.NoClip.Sprint.canceled -= StopRunning;
    }
    #endregion

    #region Unity Lifecycle
    private void FixedUpdate()
    {
        Move();
        Look();
    }
    #endregion

    #region Movement
    /// <summary>
    /// Move Input Reader
    /// </summary>
    /// <param name="ctx"></param>
    private void MoveValue(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
    
    /// <summary>
    /// Makes the player move
    /// </summary>
    private void Move()
    {
        speed = isRunning ? runSpeed : moveSpeed;
        
        Vector3 forwardDir = transform.forward * moveInput.y;
        Vector3 rightDir = transform.right * moveInput.x;
        Vector3 translationVelocity = (forwardDir + rightDir) * (speed * Time.fixedDeltaTime);
        transform.position += translationVelocity;
    }
    #endregion

    #region Look
    /// <summary>
    /// Look Input Reader
    /// </summary>
    /// <param name="ctx"></param>
    private void LookValue(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    /// <summary>
    /// Makes the player look around and clamp his ass to 90°
    /// </summary>
    private void Look()
    {
        rotY += lookInput.x;
        rotX -= lookInput.y;
        
        rotX = Mathf.Clamp(rotX, -90f, 90f);
        
        transform.localRotation = Quaternion.Euler(rotX, rotY, 0f);
    }
    #endregion

    #region Speed
    /// <summary>
    /// Speed adustment
    /// </summary>
    /// <param name="ctx"></param>
    private void StartRunning(InputAction.CallbackContext ctx)
    {
        isRunning = true;
    }

    private void StopRunning(InputAction.CallbackContext ctx)
    {
        isRunning = false;
    }
    #endregion
    #endregion
}
