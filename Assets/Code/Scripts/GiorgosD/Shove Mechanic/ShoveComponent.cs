using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine;
using System.Collections;
using PurrNet;

[RequireComponent(typeof(CharacterController))]
public class ShoveComponent : NetworkBehaviour
{
    #region Variables
    [Header("Shove Settings")]
    [SerializeField, Tooltip("Power of the shove.")] private float shoveForce;
    [SerializeField, Tooltip("How quicly the shove velocity stops. Higher valeus means faster stop time.")] private float shoveFriction;
    [SerializeField, Tooltip("From how far the Shove will be able to affect players. (Good values are 1.5 and under)")] private float shoveRange;
    [SerializeField, Tooltip("Cooldown timer of shove.")] private float shoveCooldownTimer;
    [SerializeField, Tooltip("Masks that are checked from Spherecast.")] private LayerMask checkMask;
    
    private CharacterController controller;
    private Vector3 shoveVelocity;
    public bool isInCooldown { get; private set; }
    #endregion

    #region Events
    private UnityEvent PlayerShoved;
    #endregion
    
    #region Unity lifecycle
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Applies the push if this player was pushed
    /// </summary>
    private void Update()
    {
        if (!isOwner) return;
        
        if (shoveVelocity.magnitude > 0.1f)
        {
            controller.Move(shoveVelocity * Time.deltaTime);

            shoveVelocity = Vector3.Lerp(shoveVelocity, Vector3.zero, shoveFriction * Time.deltaTime);
        }
        else
        {
            shoveVelocity = Vector3.zero;
        }
    }
    #endregion

    #region Shove
    /// <summary>
    /// Calls Shove from button press
    /// </summary>
    /// <param name="context"></param>
    public void OnShovePreformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        StartShove();
    }
    public void OnShovePreformed() => StartShove();

    /// <summary>
    /// Detects player and does the shove math
    /// </summary>
    private void StartShove()
    {
        if (isInCooldown) return;
        GetComponent<PlayerAnimatorController>()
         ?.PlayPushAnimation();
        PlayerShoved?.Invoke();
        
        Ray shoveRay = new Ray(transform.position + Vector3.up, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(shoveRay, out hit, shoveRange, checkMask, QueryTriggerInteraction.Collide))
        {
            Debug.DrawRay(transform.position + Vector3.up, transform.forward * shoveRange, Color.red);
            PlayerBody target = hit.collider.GetComponent<PlayerBody>();
            
            if (!target) return;
            
            if (target.gameObject != gameObject)
            {
                Vector3 pushDir = target.transform.position - transform.position;
                pushDir.y = 0;
                pushDir.Normalize();
                //different angles for difference in side and front/back force
                float angle = Vector3.SignedAngle(transform.forward, pushDir, Vector3.up);
                float sideFactor = Mathf.Abs(Mathf.Sin(angle * Mathf.Deg2Rad));
                float finalForce = shoveForce * Mathf.Lerp(0.5f, 1f, sideFactor);
                PushPlayer(target, pushDir * finalForce);

                StartCoroutine(CooldownShove());

            }
        }
    }

    /// <summary>
    /// Gets Shove component of other player to pass it to the RPCs
    /// </summary>
    /// <param name="player"></param>
    /// <param name="force"></param>
    private void PushPlayer(PlayerBody player, Vector3 force)
    {
        if (player.TryGetComponent<ShoveComponent>(out ShoveComponent shoveTarget))
        {
            if (player.OwnerPlayerID.HasValue)
            { 
                shoveTarget.SendShoveRPC(player.OwnerPlayerID.Value, force);
            }
        }
    }
    #endregion

    #region Shove RPCs
    /// <summary>
    /// This gets called by the server to tell the player to receive the RPC
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="force"></param>
    [ServerRpc]
    private void SendShoveRPC(PlayerID playerID, Vector3 force)
    {
        ReceiveShoveRPC(playerID, force);
    }

    /// <summary>
    /// Notifies the target player that he was shoved and passed the values
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="force"></param>
    [TargetRpc]
    private void ReceiveShoveRPC(PlayerID playerID,Vector3 force)
    {
        force.y = 0;
        shoveVelocity += force;


        FPSController fps = GetComponent<FPSController>();

        if (fps != null)
        {
            fps.IsStunned = true;
        }

        PlayerAnimatorController anim =
        GetComponent<PlayerAnimatorController>();

        if (anim != null)
        {
            anim.ApplyShove(force);
            anim.ApplyShoveLocomotion(force);
            anim.SetStunned(true);
        }

        StartCoroutine(RecoverRoutine());
    }
    #endregion

    #region Cooldown
    /// <summary>
    /// The Shove Cooldown
    /// </summary>
    /// <returns></returns>
    private IEnumerator CooldownShove()
    {
        isInCooldown = true;
        yield return new WaitForSeconds(shoveCooldownTimer);
        isInCooldown = false;
    }
    #endregion
    private IEnumerator RecoverRoutine()
    {
        yield return new WaitForSeconds(0.7f);
        FPSController fps = GetComponent<FPSController>();

        if (fps != null)
        {
            fps.IsStunned = false;
        }

        PlayerAnimatorController anim =
            GetComponent<PlayerAnimatorController>();

       /* if (anim != null)
        {
            anim.GetUp();
        }*/

        yield return new WaitForSeconds(0.3f);

        if (anim != null)
        {
            anim.SetStunned(false);
        }
    }
}
