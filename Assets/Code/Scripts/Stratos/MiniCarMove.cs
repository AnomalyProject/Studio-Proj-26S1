using UnityEngine;

public class MiniCarMove : MonoBehaviour
{
    [SerializeField] private float driveSpeed = 5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float reachDistance = 0.3f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private  Transform[] waypoints;
    private int currentIndex = 0;


    // Update is called once per frame
    void Update()
    {
        if (waypoints.Length == 0)
            return;

        Drive();
    }

    private void Drive()
    {
        Vector3 targetWorldPos = waypoints[currentIndex].position;

        targetWorldPos.y = transform.position.y;

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, driveSpeed * Time.deltaTime);

        Vector3 worldDirection = targetWorldPos - transform.position;
        if (worldDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(worldDirection);

            Quaternion correctedRotation = lookRotation * Quaternion.Euler(rotationOffset);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, correctedRotation, turnSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetWorldPos) <= reachDistance)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            int nextIndex = (i + 1) % waypoints.Length;
            if (waypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
            }
        }
    }
}
