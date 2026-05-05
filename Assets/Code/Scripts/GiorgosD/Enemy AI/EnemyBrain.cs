using PurrNet;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyBrain : NetworkBehaviour, IAlertable
{
    [Header("Patrol Settings")]
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private List<Transform> patrolPriorities = new List<Transform>();
    [SerializeField, Tooltip("How long the AI will stay idle before it continues to other stuff")] private float idleTimer;

    [Header("Chase Settings")]
    [SerializeField] private List<Transform> respawnPoints = new List<Transform>();

    private Transform targetPos;

    public enum StateID
    {
        Idle,
        Alert,
        Patrol,
        Chase,
        Attack,
        Investigate
    }
    [SerializeField] private StateID currentStateID;

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
    }

    private void Start()
    {  
        if (tempStuffEnabled)
        {
            testHelper = FindFirstObjectByType<AI_TestHelper>().GetComponent<AI_TestHelper>();

            testHelper.onWatched.AddListener(OnObservedTooMuch);

            testHelper.onItemPicked.AddListener(OnItemPicked);
        }

        ChangeState(StateID.Idle, null);
    }

    public void ChangeState(StateID newStateID, Transform target = null)
    {
        if (target != null) targetPos = target;

        currentState?.Exit();
        body.StopAll();
        currentStateID = newStateID;
        currentState = stateDictionary[newStateID];
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
        ChangeState(StateID.Alert, player);
    }

    private void OnItemPicked()
    {
        poiIsEnabled = !poiIsEnabled;
    }

    public void Alert<TTarget>(TTarget alertedBy) where TTarget : MonoBehaviour
    {
        if (currentStateID == StateID.Chase 
            || currentStateID == StateID.Attack 
            || currentStateID == StateID.Alert) return;

        ChangeState(StateID.Alert, alertedBy.transform);
    }

    private void OnDestroy()
    {
        testHelper.onItemPicked.RemoveListener(OnItemPicked);
        testHelper.onWatched.RemoveListener(OnObservedTooMuch);
    }
}