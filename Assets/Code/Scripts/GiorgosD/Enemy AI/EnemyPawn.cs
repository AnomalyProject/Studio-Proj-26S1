using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPawn : MonoBehaviour
{
    #region Enemy Settings
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    private NavMeshAgent agent;

    [Header("Sight")]
    [SerializeField] private float sightRange;
    [SerializeField] private float autoDetectRange;
    [SerializeField, Range(0, 180)] private float sightAngle;
    [SerializeField] private float checkFrequency;
    private Collider[] playersInSight = new Collider[4]; //new Collider[SessionManager.Instance.CurrentSession.Players.Count]; 
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    #endregion

    #region Events
    public event Action<Transform> OnPlayerSpotted;
    public event Action<Transform> OnPlayerAttacked;
    public event Action OnTargetReached;
    public event Action OnIdleEnd;
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
        StopAllCoroutines();
        StartCoroutine(Arrival(target));
    }

    /// <summary>
    /// Sets speed of enemy depending on the state of the enemy.
    /// </summary>
    /// <param name="isRunning"></param>
    public void SetMoveSpeed(bool isRunning)
    {
        agent.speed = isRunning ? runSpeed : walkSpeed;
    }

    /// <summary>
    /// Actually moves the enemy to the target and waits until it reaches it.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private IEnumerator Arrival(Vector3 target)
    {
        agent.SetDestination(target);

        yield return null;

        yield return new WaitUntil(() => !agent.pathPending);

        yield return new WaitUntil(() => agent.remainingDistance <= agent.stoppingDistance);

        Debug.Log("Target Reached");

        OnTargetReached?.Invoke();
    }
    #endregion

    #region Attack
    /// <summary>
    /// Attacks player.
    /// </summary>
    /// <param name="player"></param>
    public void Attack(Transform player, Transform targetPoint)
    {
        var controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.position = targetPoint.position;

        if (controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log("Player Attacked");

        OnPlayerAttacked?.Invoke(player);
    }
    #endregion

    #region Idle
    /// <summary>
    /// Makes the enemy idle.
    /// </summary>
    /// <param name="timer"></param>
    public void Idle(float timer)
    {
        StopAllCoroutines();
        agent.ResetPath();
        StartCoroutine(IdleTimer(timer));
    }

    /// <summary>
    /// gives the enemy a timer to be idle.
    /// </summary>
    /// <param name="timer"></param>
    /// <returns></returns>
    private IEnumerator IdleTimer(float timer)
    {
        yield return new WaitForSeconds(timer);
        OnIdleEnd?.Invoke();
    }
    #endregion

    #region Sight
    /// <summary>
    /// Checks for players in sight and if it finds any.
    /// </summary>
    private void Sight()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, sightRange, playersInSight, playerLayer);

        if (count == 0)
        {
            OnLostPlayer?.Invoke();
            return;
        }

        Transform closestDetectedPlayer = null;
        float minSqrDist = Mathf.Infinity;
        bool foundPlayerThisFrame = false;

        for (int i = 0; i < count; i++)
        {
            Transform player = playersInSight[i].transform;
            Vector3 directionToPlayer;
            float distance;

            if (IsPlayerDetected(player, out directionToPlayer, out distance))
            {
                foundPlayerThisFrame = true;
                float sqrDist = distance * distance;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closestDetectedPlayer = player;
                }
            }
        }

        if (closestDetectedPlayer != null && foundPlayerThisFrame)
        {
            OnPlayerSpotted?.Invoke(closestDetectedPlayer);
            Debug.Log("Player Spotted");
        }
        else
        {
            OnLostPlayer?.Invoke();
        }
    }

    /// <summary>
    /// Does dumb math stuff to check if the player is in the auto detect range or in the sight angle.
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
            return !HasObstacle(direction, distance);
        }

        return false;
    }

    /// <summary>
    /// Checks if there is an obstacle between the enemy and the player. 
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    private bool HasObstacle(Vector3 direction, float distance)
    {
        return Physics.Raycast(transform.position, direction, distance, obstacleLayer);
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
}