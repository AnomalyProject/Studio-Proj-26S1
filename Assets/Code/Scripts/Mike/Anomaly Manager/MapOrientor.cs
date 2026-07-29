using PurrNet;
using System;
using UnityEngine;

public class MapOrientor : MonoBehaviour
{
    [SerializeField] LevelExitPoint entryElevator, exitElevator;
    public static event Action<LevelExitPoint, bool> OnElevatorInteracted;
    public LevelExitPoint EntryElevator => entryElevator;
    public LevelExitPoint ExitElevator => exitElevator;

    void Awake()
    {
        entryElevator.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        entryElevator.OnExitActivated += HandleExitActivation;
        exitElevator.OnExitActivated += HandleExitActivation;
    }

    /// <summary>
    /// Change provided map's orientation and match it with the Map Orientor's exit points.
    /// </summary>
    /// <param name="map"></param>
    public void OrientMap(GameMap map) => OrientMap(map, entryElevator.transform, exitElevator.transform);

    /// <summary>
    /// Change map's orientation to match with the provided transforms.
    /// </summary>
    public static void OrientMap(GameMap map, Transform entryPoint, Transform exitPoint)
    {
        foreach(Transform child in entryPoint)
        {
            if (child.gameObject == PlayerBody.localPlayerBody.gameObject)
            {
                PlayerBody.localPlayerBody.Movement.Controller.enabled = false;
            }
            else child.gameObject.SetActive(false);
        }
        //entryPoint.ToggleChildren(childrenActive: false); // Disable all children which may be CharacterController's or Rigidbodies that tend to fight with external movement.
        entryPoint.SetPositionAndRotation(map.EntryPointAnchor.position, map.EntryPointAnchor.rotation);
        entryPoint.ToggleChildren(childrenActive: true);
        PlayerBody.localPlayerBody.Movement.Controller.enabled = true;

        exitPoint.SetPositionAndRotation(map.ExitPointAnchor.position, map.ExitPointAnchor.rotation);
    }
    private void SetNewEntryPoint(LevelExitPoint newPoint)
    {
        if (newPoint == entryElevator) return;

        LevelExitPoint temp = entryElevator;
        entryElevator = newPoint;
        exitElevator = temp;
    }

    private void HandleExitActivation(LevelExitPoint exitPoint, bool decision)
    {
        SetNewEntryPoint(exitPoint);
        OnElevatorInteracted?.Invoke(exitPoint, decision);
    }
    public void SyncEntryPointWith(NetworkID entryID, Vector3 entryPosition, Quaternion entryRotation)
    {
        if (entryElevator.id != entryID)
        {
            if (exitElevator.id == entryID)
            {
                SetNewEntryPoint(exitElevator);
            }
            else
            {
                Debug.LogWarning("Entry ID does not match any known elevator.");
                return;
            }
        }

        entryElevator.transform.SetPositionAndRotation(entryPosition, entryRotation);
    }
}