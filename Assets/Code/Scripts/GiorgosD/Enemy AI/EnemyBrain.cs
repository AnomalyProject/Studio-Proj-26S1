using System.Collections.Generic;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField, Tooltip("How long the AI will stay idle before it continues to other stuff")] private float idleTimer;

    [Header("Chase Settings")]
    [SerializeField] private List<Transform> respawnPoints = new List<Transform>();

    private EnemyPawn body;
    private BaseState currentState;

    public List<Transform> PatrolPoints => patrolPoints;
    public float IdleTime => idleTimer;
    public List<Transform> RespawnPoints => respawnPoints;

    private void Start()
    {
        body = GetComponent<EnemyPawn>();

        ChangeState(new PatrolState(this, body));
    }

    public void ChangeState(BaseState newState)
    {
        currentState?.Exit();
        body.StopAll();
        currentState = newState;
        currentState.Enter();
    }

    private void Update()
    {
        currentState?.Update();
    }
}
