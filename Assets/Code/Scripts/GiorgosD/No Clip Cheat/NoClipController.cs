using UnityEngine;
using UnityEngine.InputSystem;

public class NoClipController : MonoBehaviour
{
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
        InputBridge.Actions.NoClip.Move.performed += MoveValue;
        InputBridge.Actions.NoClip.Move.canceled += MoveValue;
        
        InputBridge.Actions.NoClip.Look.performed += LookValue;
        InputBridge.Actions.NoClip.Look.canceled += LookValue;
        
        InputBridge.Actions.NoClip.Sprint.performed += StartRunning;
        InputBridge.Actions.NoClip.Sprint.canceled += StopRunning;
    }

    private void OnDestroy()
    {
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
