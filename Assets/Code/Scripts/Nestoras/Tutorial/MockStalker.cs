using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine;

public class MockStalker : MonoBehaviour
{

    [SerializeField] private float shoveDistance;
    [SerializeField] private float brushRotationSpeed = 1000;
    [SerializeField] private float visualTurnSpeed = 8f;
    
    private PlayerBody target;
    private NavMeshAgent agent;
    private ShoveComponent shove;
    private MovingObject[] brushes;
    [SerializeField] private Transform visualsRoot;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        shove = GetComponent<ShoveComponent>();
        brushes = GetComponentsInChildren<MovingObject>();
        visualsRoot = transform.GetChild(0);

        if (PlayerBody.localPlayerBody != null) HandleLocalPlayerSpawned(PlayerBody.localPlayerBody);
        PlayerBody.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
    }
    private void OnDestroy() => PlayerBody.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
    private void HandleLocalPlayerSpawned(PlayerBody player)
    {
        if (target == null) target = player;
    }

    private void Update()
    {
        if (target == null || !agent.isOnNavMesh) return;

        foreach (var brush in brushes) brush.rotationSpeed = Mathf.Lerp(brush.rotationSpeed, shove.isInCooldown ? 0 : brushRotationSpeed, Time.deltaTime * 5);

        // Smooth visual follow
        visualsRoot.localRotation = Quaternion.Slerp(visualsRoot.localRotation, Quaternion.identity, Time.deltaTime * visualTurnSpeed);

        if (!shove.isInCooldown) agent.SetDestination(target.transform.position);

        if (Vector3.Distance(transform.position, target.transform.position) <= shoveDistance)
        {
            Quaternion oldVisualWorld = visualsRoot.rotation;

            // Instant Y-only snap
            Vector3 flatDirection = target.transform.position - transform.position;
            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flatDirection);

            // Cancel visual snap
            visualsRoot.rotation = oldVisualWorld;

            shove.OnShovePreformed(new InputAction.CallbackContext());
            agent.SetDestination(transform.position);
        }
    }
}