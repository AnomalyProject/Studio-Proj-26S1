using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBrain : NetworkBehaviour, IAlertable
{
    [Header("Patrol Settings")]
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private List<Transform> patrolPriorities = new List<Transform>();
    [SerializeField, Tooltip("How long the AI will stay idle before it continues to other stuff")] private float idleTimer;

    [Header("Chase Settings")]
    [SerializeField] private List<Transform> respawnPoints = new List<Transform>();

    public event Action<BaseState> OnStateChanged;

    private EnemyPawn body;
    private BaseState currentState;

    // TEMP stuff bc no events fire at the moment
    public bool tempStuffEnabled;
    public bool poiIsEnabled;
    private AI_TestHelper testHelper;

    public List<Transform> PatrolPoints => patrolPoints;
    public List<Transform> PatrolPriorities => patrolPriorities;
    public float IdleTime => idleTimer;
    public List<Transform> RespawnPoints => respawnPoints;

    private void Start()
    {
        body = GetComponent<EnemyPawn>();

        if (!tempStuffEnabled)
        {
            testHelper = FindFirstObjectByType<AI_TestHelper>().GetComponent<AI_TestHelper>();

            testHelper.onWatched.AddListener(OnObservedTooMuch);

            testHelper.onItemPicked.AddListener(OnItemPicked);
        }

        ChangeState(new PatrolState(this, body));
    }

    public void ChangeState(BaseState newState)
    {
        currentState?.Exit();
        body.StopAll();
        currentState = newState;
        currentState.Enter();
        OnStateChanged?.Invoke(currentState);
    }

    private void Update()
    {
        currentState?.Update();
    }

    /// <summary>
    /// Gets called when the player has been looking at the enemy for too long.
    /// </summary>
    private void OnObservedTooMuch(Transform player)
    {
        ChangeState(new AlertState(this, body, player));
    }

    private void OnItemPicked()
    {
        poiIsEnabled = !poiIsEnabled;
    }

    public void Alert<TTarget>(TTarget alertedBy) where TTarget : MonoBehaviour
    {
        if (currentState is ChaseState || currentState is AttackState || currentState is AlertState) return;

        ChangeState(new AlertState(this, body, alertedBy.transform));
    }

    private void OnDestroy()
    {
        testHelper.onItemPicked.RemoveListener(OnItemPicked);
        testHelper.onWatched.RemoveListener(OnObservedTooMuch);
    }
}