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
    public void OrientMap(GameMap map) => OrientMap(map, entryElevator, exitElevator);

    /// <summary>
    /// Change map's orientation to match with the provided transforms.
    /// </summary>
    public static void OrientMap(GameMap map, LevelExitPoint entryPoint, LevelExitPoint exitPoint)
    {
        entryPoint.HandleTransportedChilds(childsActive: false);
        entryPoint.transform.SetPositionAndRotation(map.EntryPointAnchor.position, map.EntryPointAnchor.rotation);
        entryPoint.HandleTransportedChilds(childsActive: true);

        exitPoint.transform.SetPositionAndRotation(map.ExitPointAnchor.position, map.ExitPointAnchor.rotation);
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