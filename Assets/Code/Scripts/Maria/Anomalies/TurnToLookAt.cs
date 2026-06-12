using UnityEngine;
using PurrNet;

/// <summary>
/// Rotates this object to always face the nearest player.
/// </summary>
public class TurnToLookAt : MonoBehaviour
{
    #region Serialized Fields
    [Header("Facing Settings")]
    [Tooltip("Degrees per second to rotate toward the target. Set to 0 for instant snap.")]
    [SerializeField] private float rotationSpeed = 5f;

    [Tooltip("Lock the Y axis so the object only yaws (useful for upright NPCs/billboards).")]
    [SerializeField] private bool yawOnly = true;

    [Tooltip("How often (seconds) to re-scan for the nearest player.")]
    [SerializeField] private float targetRefreshInterval = 0.5f;
    #endregion

    #region Private Fields
    private Transform target;
    private float nextRefresh;
    #endregion

    #region Unity Callbacks
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

        if (yawOnly)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        // 180° Y offset compensates for meshes modelled with Z− as front. Because they can't import shit correctly >=[
        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, -90f, 0f);

        transform.rotation = rotationSpeed > 0f
            ? Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime)
            : targetRotation;
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

        foreach (PlayerBody identity in FindObjectsByType<PlayerBody>(FindObjectsSortMode.None))
        {
            float sqDist = (identity.transform.position - transform.position).sqrMagnitude;
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