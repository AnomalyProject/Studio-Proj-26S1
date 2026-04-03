using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPawn : MonoBehaviour
{
    #region Enemy Settings
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    public NavMeshAgent agent { get; private set; }

    [Header("Sight")]
    [SerializeField, Tooltip("How far in front of it can see")] private float sightRange;
    [SerializeField, Tooltip("How close the player need to be for the AI to cinsider him 'touch' distance")] private float autoDetectRange;
    [SerializeField, Range(0, 180), Tooltip("Gives the designer te ability to set the how wide the AIs sight is in rad")] private float sightAngle;
    [SerializeField,Tooltip("How often it should check for what it sees")] private float checkFrequency;
    private Collider[] playersInSight = new Collider[4]; //new Collider[SessionManager.Instance.CurrentSession.Players.Count]; 
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    private Transform cachedPlayer;

    [Header("Attack")]
    [SerializeField, Tooltip("Controls size of the hitbox")] private Vector3 attackHitBox;
    [SerializeField, Tooltip("Controls how far in front the hitbox will be")] private float attackOffset;

    [Header("Turning")]
    [SerializeField] private float turnThreshold;
    #endregion

    #region Events
    public event Action<GameObject> OnPlayerSpotted;
    public event Action OnLostPlayer;
    #endregion

    #region Body Set up
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        InvokeRepeating(nameof(Sight), 0f, checkFrequency);
    }
    #endregion

    #region Movement
    /// <summary>
    /// Tells the enemy to move to the target possition.
    /// </summary>
    /// <param name="target"> either the player or the point whatever the brain thinks </param>
    public void MoveToTarget(Vector3 target) 
    { 
        agent.SetDestination(target);
    }

    /// <summary>
    /// Sets speed of enemy depending on the state of the enemy.
    /// </summary>
    /// <param name="isRunning"></param>
    public void SetMoveSpeed(bool isRunning)
    {
        agent.speed = isRunning ? runSpeed : walkSpeed;
    }
    #endregion

    #region Attack
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
    #endregion

    #region Sight
    /// <summary>
    /// Checks for players in sight and if it finds any.
    /// </summary>
    private void Sight()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, sightRange, playersInSight, playerLayer);

        Transform closestDetectedPlayer = null;
        float minSqrDist = Mathf.Infinity;

        for (int i = 0; i < count; i++)
        {
            Transform player = playersInSight[i].transform;
            if (IsPlayerDetected(player, out Vector3 direction, out float distance))
            {
                float sqrDist = distance * distance;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closestDetectedPlayer = player;
                }
            }
        }

        if (closestDetectedPlayer != null)
        {
            if (cachedPlayer == null || cachedPlayer != closestDetectedPlayer)
            {
                cachedPlayer = closestDetectedPlayer;
                OnPlayerSpotted?.Invoke(cachedPlayer.gameObject);
                Debug.Log($"Target Locked: {cachedPlayer.name}");
            }
        }
        else
        {
            if (cachedPlayer != null)
            {
                cachedPlayer = null;
                OnLostPlayer?.Invoke();
                Debug.Log("Target Lost.");
            }
        }
    }

    /// <summary>
    /// Checks if the enemy can actually see the player.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    private bool IsPlayerDetected(Transform player, out Vector3 direction, out float distance)
    {
        Vector3 offset = player.position - transform.position;
        distance = offset.magnitude;
        direction = offset / distance;

        bool inAutoRange = distance <= autoDetectRange;

        float thresholdAngle = Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad);
        bool inSightAngle = Vector3.Dot(transform.forward, direction) > thresholdAngle;

        if (inAutoRange || inSightAngle)
        {
            return !Physics.Raycast(transform.position, direction, distance, obstacleLayer);
        }

        return false;
    }
    #endregion

    #region Stop All
    /// <summary>
    /// Safty stops evrything gives brain more control.
    /// </summary>
    public void StopAll()
    {
        StopAllCoroutines();
        agent.ResetPath();
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
}