using PurrNet;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyBrain : NetworkBehaviour, IAlertable
{
    public enum StateID
    {
        Idle,
        Alert,
        Patrol,
        Chase,
        Attack,
        Investigate,
        Stunned
    }
    [Header("STATE")]
    [SerializeField] private StateID currentStateID;

    [Header("Patrol Settings")]
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private List<Transform> patrolPriorities = new List<Transform>();
    [SerializeField, Tooltip("How long the AI will stay idle before it continues to other stuff")] private float idleTimer;

    [Header("Chase Settings")]
    [SerializeField] private List<Transform> respawnPoints = new List<Transform>();

    private Transform targetPos;

    private Dictionary<StateID, BaseState> stateDictionary = new Dictionary<StateID, BaseState>();

    private EnemyPawn body;
    private BaseState currentState;

    public event Action<BaseState> OnStateChanged;

    // TEMP stuff bc no events fire at the moment
    public bool tempStuffEnabled;
    public bool poiIsEnabled;
    private AI_TestHelper testHelper;
    // TEMP END

    public List<Transform> PatrolPoints => patrolPoints;
    public List<Transform> PatrolPriorities => patrolPriorities;
    public Transform TargetPos => targetPos;
    public float IdleTime => idleTimer;
    public List<Transform> RespawnPoints => respawnPoints;

    private void Awake()
    {
        body = GetComponent<EnemyPawn>();

        stateDictionary.Add(StateID.Idle, new IdleState(this, body));
        stateDictionary.Add(StateID.Alert, new AlertState(this, body));
        stateDictionary.Add(StateID.Patrol, new PatrolState(this, body));
        stateDictionary.Add(StateID.Chase, new ChaseState(this, body));
        stateDictionary.Add(StateID.Attack, new AttackState(this, body));
        stateDictionary.Add(StateID.Investigate, new InvestigateState(this, body));
        stateDictionary.Add(StateID.Stunned, new StunnedState(this, body));
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        // TEMP
        if (tempStuffEnabled)
        {
            testHelper = FindFirstObjectByType<AI_TestHelper>().GetComponent<AI_TestHelper>();

            testHelper.onWatched.AddListener(OnObservedTooMuch);

            testHelper.onItemPicked.AddListener(OnItemPicked);
        }
        // TEMP END

        if (!isServer) return;

        // Warp enemy to a random patrol point on spawn, if there are any
        if (patrolPoints.Count > 0) body.agent.Warp(patrolPoints[UnityEngine.Random.Range(0, patrolPoints.Count)].position);

        ChangeState(StateID.Idle);
    }

    public void ChangeState(StateID newStateID, Transform target = null)
    {
        if (!isServer) return;

        if (target != null) targetPos = target;

        currentState?.Exit();
        currentStateID = newStateID;
        currentState = stateDictionary[newStateID];
        currentState.Enter();
        OnStateChanged?.Invoke(currentState);

        Debug.Log($"State changed to {newStateID}");
    }

    private void Update()
    {
        if (!isServer) return;

        currentState?.Update();
    }

    /// <summary>
    /// Gets called when the player has been looking at the enemy for too long.
    /// </summary>
    private void OnObservedTooMuch(Transform player)
    {
        if (!isServer) return;

        ChangeState(StateID.Alert, player);
    }

    /// <summary>
    /// Gets Called when the player picks up the item the enemy is interested in.
    /// </summary>
    private void OnItemPicked()
    {
        if (!isServer) return;

        poiIsEnabled = !poiIsEnabled;
    }

    public void Alert<TTarget>(TTarget alertedBy) where TTarget : MonoBehaviour
    {
        if (!isServer) return;

        if (currentStateID == StateID.Chase 
            || currentStateID == StateID.Attack 
            || currentStateID == StateID.Alert
            || currentStateID == StateID.Investigate) return;

        Debug.Log($"[EnemyBrain] {gameObject.name} audibly alerted by {alertedBy.gameObject.name}");
        ChangeState(StateID.Alert, alertedBy.transform);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (tempStuffEnabled)
        {
            testHelper.onItemPicked.RemoveListener(OnItemPicked);
            testHelper.onWatched.RemoveListener(OnObservedTooMuch);
        }
    }
}