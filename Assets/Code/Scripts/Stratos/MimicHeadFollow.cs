using UnityEngine;
using PurrNet;
public class MimicHeadFollow : MonoBehaviour
{
    [SerializeField] private Transform neckBone;

    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private bool fullRotation = false;
    [SerializeField] private float maxTurnAngle = 90f;

    [SerializeField] private float targetRefreshInterval = 0.5f;

    private Transform currentTargetPlayer;
    [SerializeField] private float nextRefresh;
    private Quaternion initialLocalRotation;
    [SerializeField] private bool hasSavedRotation = false;

    void Start()
    {
        TryFindNeckBone();
    }

    void Update()
    {
        if (Time.time >= nextRefresh)
        {
            FindClosestPlayer();
            nextRefresh = Time.time + targetRefreshInterval;
        }

        if (currentTargetPlayer != null)
        {
            if (neckBone == null)
            {
                TryFindNeckBone();
                if (neckBone == null) return;
            }

            if (!hasSavedRotation)
            {
                initialLocalRotation = neckBone.localRotation;
                hasSavedRotation = true;
            }

            Vector3 targetPos = currentTargetPlayer.position;
            targetPos.y += 1.6f;

            Vector3 directionToPlayer = targetPos - neckBone.position;

            if (!fullRotation)
            {
                float angle = Vector3.Angle(transform.forward, directionToPlayer);
                if (angle > maxTurnAngle)
                {
                    ReturnHeadToNormal();
                    return;
                }
            }
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            Quaternion finalRotation = lookRotation * Quaternion.Euler(rotationOffset);

            neckBone.rotation = Quaternion.Slerp(neckBone.rotation, finalRotation, Time.deltaTime * turnSpeed);
        }
        else
        {
            ReturnHeadToNormal();
        }
    }

    void ReturnHeadToNormal()
    {
        if (neckBone == null || !hasSavedRotation) return;
        Quaternion targetWorldRotation = transform.rotation * initialLocalRotation;
        neckBone.rotation = Quaternion.Slerp(neckBone.rotation, targetWorldRotation, Time.deltaTime * turnSpeed);
    }

    void TryFindNeckBone()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == "mixamorig:Neck")
            {
                neckBone = child;
                initialLocalRotation = neckBone.localRotation;
                hasSavedRotation = true;
                return;
            }
        }
    }

    void FindClosestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (PlayerBody player in FindObjectsByType<PlayerBody>(FindObjectsSortMode.None))
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < closestDistance && distance <= maxDistance)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }
        currentTargetPlayer = closestPlayer;
    }
}

    


