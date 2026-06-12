using PurrNet;
using UnityEngine;
[RequireComponent(typeof(NetworkTransform))]
public class MovingObject : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField, Tooltip("Applies Movement")] private bool doesMove = true;
    [SerializeField, Min(0.01f), Tooltip("Movement duration from point A to point B.")] private float moveDuration = 2f;
    [SerializeField, Tooltip("Controls easing of the movement.")] private AnimationCurve movementCurve;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;


    [Header("Rotation")]
    [SerializeField, Tooltip("Applies Rotation")] private bool doesRotate = true;
    [SerializeField, Min(0)] private float rotationSpeed;
    [SerializeField] private Vector3 rotationAxis;

    private float moveTime;
    private bool goingForward = true;

    private void Update()
    {
        if (!isServer) return;
        DoMove(Time.deltaTime);
        DoRotate(Time.deltaTime);
    }

    private void DoMove(float delta)
    {
        if (!doesMove) return;
        if (pointA == null || pointB == null) return;

        moveTime += delta / moveDuration;
        float t = Mathf.Clamp01(moveTime);
        float curveT = movementCurve.Evaluate(t);

        Vector3 start = goingForward? pointA.position : pointB.position;
        Vector3 end = goingForward? pointB.position : pointA.position;

        transform.position = Vector3.Lerp(start, end, curveT);

        if (t >= 1f)
        {
            moveTime = 0f;
            goingForward = !goingForward;
        }
    }

    private void DoRotate(float delta)
    {
        if (!doesRotate) return;
        transform.Rotate(rotationAxis * rotationSpeed * delta, Space.World);
    }

    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pointA.position, 0.15f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pointB.position, 0.15f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(pointA.position, pointB.position);
    }
}