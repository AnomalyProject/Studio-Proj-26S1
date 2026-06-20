using UnityEngine;
using PurrNet;
public class MimicHeadFollow : MonoBehaviour
{
    /*
     /// <summary>
     /// ////////////////////////////////////// 1st try 
     /// </summary>

     [SerializeField] private string playerTag = "Player";
     [SerializeField] private Transform neckBone;
     [SerializeField] private float turnSpeed = 5f;
     [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 0f, 0f);
     [SerializeField] private float maxDistance = 15f;
     [SerializeField] private bool fullRotation = false;
     [SerializeField] private float maxTurnAngle = 90f;

     private Transform targetPlayer;
     private Quaternion initialLocalRotation;


     // Start is called once before the first execution of Update after the MonoBehaviour is created
     void Start()
     {
         initialLocalRotation = neckBone.localRotation;
     }

     // Update is called once per frame
     void Update()
     {
         if (neckBone = null)
             return;

         FindClosestPlayer();

         if (targetPlayer != null)
         {
             Vector3 targetPos = targetPlayer.position;
             targetPos.y += 1.6f;

             Vector3 directionToPlayer = targetPos - neckBone.position;

             if (fullRotation)
             {
                 float angle = Vector3.Angle(transform.forward, directionToPlayer);
                 if (angle > maxTurnAngle)
                 {
                     ReturnHeadToNormal();
                     return;
                 }

                 Quaternion lookRotatation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                 Quaternion finalRotation = lookRotatation * Quaternion.Euler(rotationOffset);

                 neckBone.rotation = Quaternion.Slerp(neckBone.rotation, finalRotation, Time.deltaTime * turnSpeed);
             }
         }
     }


     void ReturnHeadToNormal()
     {
         Quaternion targetWorldRotation = transform.rotation * initialLocalRotation;
         neckBone.rotation = Quaternion.Slerp(neckBone.rotation, targetWorldRotation, Time.deltaTime * turnSpeed);
     }

     void FindClosestPlayer()
     {
         GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
         float closestDistance = Mathf.Infinity;
         GameObject closestPlayer = null;

         foreach (GameObject player in players)
         {
             float distance = Vector3.Distance(transform.position, player.transform.position);

             if (distance < closestDistance && distance <= maxDistance)
             {
                 closestDistance = distance;
                 closestPlayer = player;
             }
         }
         targetPlayer = closestPlayer != null ? closestPlayer.transform : null;
     }
   */

    //////////////////////////2nd try 

    [Header("Target Setup")]
    public Transform neckBone;

    [Header("Tracking Settings")]
    public float turnSpeed = 5f;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Distance Limits")]
    public float maxDistance = 5f;
    public bool fullRotation = false;
    public float maxTurnAngle = 90f;

    [Header("Performance Upgrades")]
    [Tooltip("How often (seconds) to re-scan for the nearest player.")]
    public float targetRefreshInterval = 0.5f;

    private Transform currentTargetPlayer;
    private float nextRefresh;
    private Quaternion initialLocalRotation;
    private bool hasSavedRotation = false;

    void Start()
    {
        TryFindNeckBone();
    }

    void Update()
    {
        // 1. THE TIMER UPGRADE: Only scan the room twice a second instead of every frame!
        if (Time.time >= nextRefresh)
        {
            FindClosestPlayer();
            nextRefresh = Time.time + targetRefreshInterval;
        }

        // 2. THE ROTATION LOGIC
        if (currentTargetPlayer != null)
        {
            // The Ironclad Null Shield
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

    // 3. THE COMPONENT SEARCH UPGRADE
    void FindClosestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        // FindObjectsSortMode.None makes the search slightly faster
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

    


