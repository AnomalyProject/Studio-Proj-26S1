using UnityEngine;
using PurrNet;

/// <summary>
/// Rotates this object to face the nearest player within configurable
/// horizontal and vertical angle constraints, with a per-axis mesh
/// correction offset baked in via <see cref="ForwardAxis"/>.
/// </summary>
public class TurnToLookAt : MonoBehaviour
{
    #region Enums

    public enum ForwardAxis
    {
        ZPositive,   // Standard Unity forward — no correction needed
        ZNegative,   // Mesh front faces Z−  (180° Y offset)
        XPositive,   // Mesh front faces X+  (-90° Y offset)
        XNegative,   // Mesh front faces X−  ( 90° Y offset)
        YPositive,   // Mesh front faces Y+  (-90° X offset)
        YNegative    // Mesh front faces Y−  ( 90° X offset)
    }

    #endregion

    #region Serialized Fields

    [Header("Facing Settings")]
    [Tooltip("Which local axis the mesh considers its 'front'. Adjusts the rotation offset so LookRotation lands on the right face.")]
    [SerializeField] private ForwardAxis meshForwardAxis = ForwardAxis.XPositive;

    [Tooltip("Degrees per second to rotate toward the target. Set to 0 for instant snap.")]
    [SerializeField] private float rotationSpeed = 5f;

    [Tooltip("How often (seconds) to re-scan for the nearest player.")]
    [SerializeField] private float targetRefreshInterval = 0.5f;

    [Header("Angle Constraints")]
    [Tooltip("Maximum degrees left or right from the rest pose. 180 = full yaw freedom.")]
    [SerializeField, Range(0f, 180f)] private float horizontalLimit = 180f;

    [Tooltip("Maximum degrees up or down from the rest pose. 90 = full pitch freedom.")]
    [SerializeField, Range(0f, 90f)] private float verticalLimit = 90f;

    #endregion

    #region Private Fields

    private Transform target;
    private float nextRefresh;

    // The object's resting rotation captured at Start; all angle constraints
    // are measured relative to this so placement in the scene doesn't matter.
    private Quaternion restRotation;

    #endregion

    #region Unity Callbacks

    private void Awake() => restRotation = transform.rotation;

    private void OnDisable() => transform.rotation = restRotation;

    private void Update()
    {
        if (Time.time >= nextRefresh)
        {
            target = FindNearestPlayer();
            nextRefresh = Time.time + targetRefreshInterval;
        }

        if (target == null) return;

        FaceTarget(target.position);
    }

    #endregion

    #region Facing

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion rawRotation = Quaternion.LookRotation(direction) * GetMeshOffset();
        Quaternion clamped = ClampRotation(rawRotation);

        transform.rotation = rotationSpeed > 0f
            ? Quaternion.Slerp(transform.rotation, clamped, rotationSpeed * Time.deltaTime)
            : clamped;
    }

    /// <summary>
    /// Returns the quaternion offset that corrects for the mesh's modelled
    /// forward axis so that LookRotation always ends up pointing the right face
    /// at the target.
    /// </summary>
    private Quaternion GetMeshOffset()
    {
        return meshForwardAxis switch
        {
            ForwardAxis.ZPositive => Quaternion.identity,
            ForwardAxis.ZNegative => Quaternion.Euler(0f, 180f, 0f),
            ForwardAxis.XPositive => Quaternion.Euler(0f, -90f, 0f),
            ForwardAxis.XNegative => Quaternion.Euler(0f, 90f, 0f),
            ForwardAxis.YPositive => Quaternion.Euler(-90f, 0f, 0f),
            ForwardAxis.YNegative => Quaternion.Euler(90f, 0f, 0f),
            _ => Quaternion.identity
        };
    }

    /// <summary>
    /// Clamps <paramref name="desired"/> so it never exceeds
    /// <see cref="horizontalLimit"/> or <see cref="verticalLimit"/>
    /// degrees away from the rest pose.
    /// </summary>
    private Quaternion ClampRotation(Quaternion desired)
    {
        // Express desired rotation in rest-pose local space so the limits
        // are always relative to where the object started, regardless of
        // how it's placed in the world.
        Quaternion localDelta = Quaternion.Inverse(restRotation) * desired;
        localDelta.ToAngleAxis(out float angle, out Vector3 axis);

        // Decompose into yaw (Y) and pitch (X) in local space.
        Vector3 euler = localDelta.eulerAngles;
        float yaw = WrapAngle(euler.y);
        float pitch = WrapAngle(euler.x);

        yaw = Mathf.Clamp(yaw, -horizontalLimit, horizontalLimit);
        pitch = Mathf.Clamp(pitch, -verticalLimit, verticalLimit);

        Quaternion clampedLocal = Quaternion.Euler(pitch, yaw, 0f);
        return restRotation * clampedLocal;
    }

    /// <summary>
    /// Remaps a Unity euler angle from [0, 360) to (-180, 180] so
    /// Mathf.Clamp works symmetrically around zero.
    /// </summary>
    private static float WrapAngle(float angle)
    {
        angle %= 360f; // angle = angle % 360 is not needed.
        return angle > 180f ? angle - 360f : angle;
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// Returns the transform of the PlayerBody closest to this object.
    /// </summary>
    private Transform FindNearestPlayer()
    {
        Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        foreach (PlayerBody identity in PlayerBody.ActivePlayers)
        {
            if(identity == null || identity.CameraController == null) continue;

            float sqDist = (identity.CameraController.transform.position - transform.position).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = identity.transform;
            }
        }

        return nearest;
    }

    #endregion
}