using PurrNet;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyPawn : NetworkBehaviour
{
    #region Enemy Settings
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    public NavMeshAgent agent { get; private set; }
    public NavMeshPath path { get; private set; }
    public NetworkAnimator anim { get; private set; }

    [Header("Sight")]
    [SerializeField, Tooltip("How far in front of it can see")] private float sightRange;
    [SerializeField, Tooltip("How close the player need to be for the AI to cinsider him 'touch' distance")] private float autoDetectRange;
    [SerializeField, Range(0, 180), Tooltip("Gives the designer te ability to set the how wide the AIs sight is in rad")] private float sightAngle;
    [SerializeField, Range(0, 180), Tooltip("How wide the AIs sight is when searching for the player. (Idle uses it to mock a looking around with its head anim)")] private float sightAngleSearch;
    private float sightAngleNormal;

    [SerializeField, Tooltip("The offset point (Y) where the raycast start (preferably its head)")] private Transform eyePos;
    private Collider[] playersInSight = new Collider[4]; 
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    private PlayerBody cachedPlayer;
    
    [Header("Lost Player Timer")]
    [SerializeField, Tooltip("How much time does it take for the ai to lose you and enter investigate after the olayer moves out of sight.")]private float timeToLost = 2.0f;
    private bool hasPlayer = false;
    private float timer;
    
    // Aggression will be revised later.
    [Header("Aggression Settings")]
    [SerializeField, Tooltip("Controls how much each aggression level increases the run speed")] private float runMultiplier;
    [SerializeField, Tooltip("Controls how much each aggression level increases the auto Detection")] private float autoDetectMultiplier;
    [SerializeField, Tooltip("Controls how much each aggression level increases the Sight Range")] private float sightRangeMultiplier;
    [SerializeField, Tooltip("Controls the current aggression level of the enemy")] private int aggressionLevel;
    [SerializeField, Tooltip("Controls the maximum aggression level the enemy can reach")] private int maxAggressionLevel;

    [Header("Attack")]
    [SerializeField, Tooltip("Controls size of the hitbox")] private Vector3 attackHitBox;
    [SerializeField, Tooltip("Controls how far in front the hitbox will be")] private float attackOffset;
    #endregion
    
    #region Events
    public UnityEvent<PlayerBody> OnPlayerSpotted;
    public UnityEvent OnLostPlayer;
    public UnityEvent OnStartAttack;
    public UnityEvent OnEndAttack;
    public UnityEvent<PlayerBody> OnPlayerAttacked;
    #endregion

    #region Body Set up
    private void Awake()
    {
        anim = GetComponentInChildren<NetworkAnimator>();
        agent = GetComponent<NavMeshAgent>();
        path = new();
        sightAngleNormal = sightAngle;
    }
    
    private void Update()
    {
        if (!isServer) return;

        Sight();

        if (!hasPlayer)
        {
            LostTimer();
        }
    }
    #endregion
    
    #region Movement
    /// <summary>
    /// Tells the enemy to move to the target possition.
    /// </summary>
    /// <param name="target"> either the player or the point whatever the brain thinks </param>
    public void MoveToTarget(Vector3 target)
    {
        if (!isServer) return;

        //Debug.Log($"Moving to {target}");

        if (Vector3.Distance(agent.destination, target) > 0.9f) agent.SetDestination(target);
    }

    /// <summary>
    /// Sets speed of enemy depending on the state of the enemy. (true = running, false = walking)
    /// </summary>
    /// <param name="isRunning"></param>
    public void SetMoveSpeed(bool isRunning)
    {
        agent.speed = isRunning ? runSpeed : walkSpeed;
    }

    #endregion

    #region Attack
    /// <summary>
    /// Tells attack state to fire attack
    /// </summary>
    public void StartAttack()
    {
        InvokeStartAttack();
    }

    /// <summary>
    /// Tells attack state to fire change state
    /// </summary>
    public void EndAttack()
    {
        InvokeEndAttack();
    }
    
    /// <summary>
    /// Checks if player is in attack range.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public bool IsHitSuccess(Transform player)
    {
        Vector3 hitboxCenter = transform.position + (transform.forward * attackOffset);

        Collider[] hitColliders = Physics.OverlapBox(hitboxCenter, attackHitBox / 2, transform.rotation, playerLayer);

        return Array.Exists(hitColliders, c => c.transform == player);
    }

    /// <summary>
    /// It teleports the player to a random spawn point when attacked by the enemy.
    /// </summary>
    /// <param name="spawn"></param>
    /// <param name="player"></param>
    [ObserversRpc]
    public void TeleportToSpawn(Vector3 spawn, NetworkIdentity player)
    {
        if (!player.isOwner || player.transform.position == null) return;

        var controller = player.GetComponent<CharacterController>();

        if (controller != null) controller.enabled = false;

        player.transform.position = spawn;

        if (controller != null) controller.enabled = true;

    }
    #endregion

    #region Aggression
    /// <summary>
    /// Increasses the aggression level of the enemy by 1 and updates all the stats accordingly. (Later will change to listen to event)
    /// </summary>
    public void IncreaseAggression()
    {
        if (aggressionLevel < maxAggressionLevel)
        {
            aggressionLevel++;
            runSpeed *= runMultiplier;
            autoDetectRange *= autoDetectMultiplier;
            sightRange *= sightRangeMultiplier;
            //checkFrequency -= checkFrequencyReduction;

            Debug.Log($"Aggression increased to level {aggressionLevel}");
            return;
        }

        Debug.LogWarning("Aggression level is already at maximum!");
    }

    /// <summary>
    /// Does the opposite of IncreaseAggression.
    /// </summary>
    public void DecreaseAggression()
    {
        if (aggressionLevel > 0)
        {
            aggressionLevel--;
            runSpeed /= runMultiplier;
            autoDetectRange /= autoDetectMultiplier;
            sightRange /= sightRangeMultiplier;
            //checkFrequency += checkFrequencyReduction;
            Debug.Log($"Aggression decreased to level {aggressionLevel}");
            return;
        }
        Debug.LogWarning("Aggression level is already at minimum!");
    }
    #endregion

    #region Turning
    /// <summary>
    /// Makes the enemy always face the player.
    /// Needed cause sometimes the player can outmaneuver the enemy and stay behind it without the enemy being able to turn around to get him putting them in a stalemate.
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    public void RotateTowards(Vector3 targetPos)
    {
        if (!isServer) return;

        Vector3 direction = (targetPos - transform.position);
        direction.y = 0;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            agent.angularSpeed * Time.deltaTime
        );

        agent.isStopped = false;
    }

    /// <summary>
    /// Check for if enemy is facing target.
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    public bool IsFacingTarget(Vector3 targetPos)
    {
        Vector3 directionToTarget = (targetPos - transform.position).normalized;

        directionToTarget.y = 0;

        float dotProduct = Vector3.Dot(transform.forward.normalized, directionToTarget.normalized);

        return dotProduct >= 0.95f;
    }
    #endregion
    
    #region Sight Lost Timer
    /// <summary>
    /// A timer that checks when the ai actually should lose the player and stop following his live pos
    /// </summary>
    private void LostTimer()
    {
        if (!isServer) return;
        
        if (cachedPlayer == null) return;

        timer += Time.deltaTime;

        if (timer >= timeToLost)
        {
            hasPlayer = false;
            cachedPlayer = null;
            timer = 0f;
            InvokeOnLost();
        }
    }
    #endregion

    #region Target Reachability Check
    /// <summary>
    /// Checks if it can reach the player
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    public bool IsTargetReachable(Vector3 targetPos)
    {
        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return false;

        if (!agent.CalculatePath(hit.position, path))
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }
    #endregion
    
    #region Sight
    /// <summary>
    /// Checks for players in sight and if it finds any.
    /// </summary>
    private void Sight()
    {
        if (!isServer) return;
        
        if (cachedPlayer != null)
        {
            if (IsTargetVisible(cachedPlayer.transform) && IsTargetReachable(cachedPlayer.transform.position))
            {
                hasPlayer = true;
                timer = 0f;
                InvokeSpotted(cachedPlayer);
                return;
            }
            
            Vector3 targetDestination = cachedPlayer.transform.position;
            if (cachedPlayer.TryGetComponent<Collider>(out Collider col))
            {
                targetDestination = col.bounds.center;
            }

            float distanceToTarget = Vector3.Distance(eyePos.position, targetDestination);
            if (distanceToTarget <= autoDetectRange)
            {
                if (!Physics.Raycast(eyePos.position, (targetDestination - eyePos.position).normalized, distanceToTarget, obstacleLayer))
                {
                    hasPlayer = true;
                    timer = 0f;
                    InvokeSpotted(cachedPlayer);
                    return;
                }
            }
            
            hasPlayer = false;
            return;
        }
        
        int count = Physics.OverlapSphereNonAlloc(transform.position, sightRange, playersInSight, playerLayer);
        
        for (int i = 0; i < count; i++)
        {
            PlayerBody player = playersInSight[i].GetComponent<PlayerBody>();

            if (IsTargetVisible(player.transform) && IsTargetReachable(player.transform.position))
            {
                cachedPlayer = player;
                hasPlayer = true;
                timer = 0f;
                InvokeSpotted(cachedPlayer);
                break; 
            }
        }
    }

    /// <summary>
    /// Checks if player is visible in the view cone or autodetect range
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool IsTargetVisible(Transform target)
    {
        Vector3 targetDestination = target.position;
        if (target.TryGetComponent<Collider>(out Collider col))
        {
            targetDestination = col.bounds.center;
        }

        float distanceToTarget = Vector3.Distance(eyePos.position, targetDestination);
        
        if (distanceToTarget <= autoDetectRange)
        {
            if (!Physics.Raycast(eyePos.position, (targetDestination - eyePos.position).normalized, distanceToTarget, obstacleLayer))
            {
                return true; 
            }
        }
        
        Vector3 directionToTarget = (targetDestination - eyePos.position).normalized;
        
        Vector3 flatForward = transform.forward;
        flatForward.y = 0;
        flatForward.Normalize();

        Vector3 flatDirection = directionToTarget;
        flatDirection.y = 0;
        flatDirection.Normalize();
        
        if (Vector3.Angle(flatForward, flatDirection) < sightAngle / 2f)
        {
            if (!Physics.Raycast(eyePos.position, directionToTarget, distanceToTarget, obstacleLayer))
            {
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// This func increases and decreases the Sight angle to mimic the enemy looking around for the player when it loses sight.
    /// </summary>
    [ObserversRpc]
    public void Search(bool isSearching)
    {
        if (isSearching)
        {
            sightAngle = sightAngleSearch;
        }
        else
        {
            sightAngle = sightAngleNormal;
        }
    }
    #endregion

    #region Stop All
    /// <summary>
    /// Safty stops evrything gives brain more control.
    /// </summary>
    public void StopAll()
    {
        if (!isServer) return;

        StopAllCoroutines();
        if (agent.hasPath) agent.ResetPath();
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        // Autodetect range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoDetectRange);

        // Max sight range (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Sight angle (red lines)
        Vector3 rightLimit = Quaternion.AngleAxis(sightAngle * 0.5f, Vector3.up) * transform.forward;
        Vector3 leftLimit = Quaternion.AngleAxis(-sightAngle * 0.5f, Vector3.up) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, rightLimit * sightRange);
        Gizmos.DrawRay(transform.position, leftLimit * sightRange);

        // Attack hitbox (green box)
        Gizmos.color = Color.green;
        Vector3 hitboxCenter = transform.position + (transform.forward * attackOffset);
        Gizmos.matrix = Matrix4x4.TRS(hitboxCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackHitBox);
    }
    #endregion
    
    #region Event Helpers
    /// <summary>
    /// Invokes Spotted Helper
    /// </summary>
    [ObserversRpc]
    private void InvokeSpotted(PlayerBody player)
    {
        OnPlayerSpotted?.Invoke(player);
    }
    
    /// <summary>
    /// Invokes OnLost Helper
    /// </summary>
    [ObserversRpc]
    private void InvokeOnLost()
    {
        OnLostPlayer?.Invoke();
    }

    /// <summary>
    /// Invokes Start Attack
    /// </summary>
    /// <param name="player"></param>
    [ObserversRpc]
    private void InvokeStartAttack()
    {
        OnStartAttack?.Invoke();
    }

    /// <summary>
    /// Invokes End Attack
    /// </summary>
    [ObserversRpc]
    private void InvokeEndAttack()
    {
        OnEndAttack?.Invoke();
    }
    
    /// <summary>
    /// Invokes Attack Helper
    /// </summary>
    [ObserversRpc]
    public void InvokeAttacked(PlayerBody player)
    { 
        OnPlayerAttacked?.Invoke(player);
    }
    #endregion
    #region Stun
    public float StunDuration { get; private set; }

    public void SetStunDuration(float durationSeconds)
    {
        StunDuration = durationSeconds;
    }
    #endregion
}
