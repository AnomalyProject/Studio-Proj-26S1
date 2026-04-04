using System;
using UnityEngine;

public class MapOrientor : MonoBehaviour
{
    [SerializeField] LevelExitPoint entryElevator, exitElevator;
    public event Action<LevelExitPoint, bool> OnElevatorInteracted;
    public LevelExitPoint EntryElevator => entryElevator;
    public LevelExitPoint ExitElevator => exitElevator;

    void Awake()
    {
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
        // Entry Point is the parent of the whole map, configured in GameMap's awake.
        map.EntryPointAnchor.position = entryPoint.transform.position;
        map.EntryPointAnchor.rotation = entryPoint.transform.rotation;

        exitPoint.transform.position = map.ExitPointAnchor.position;
        exitPoint.transform.rotation = map.ExitPointAnchor.rotation;
    }
    void SetNewEntryPoint(LevelExitPoint newPoint)
    {
        if (newPoint == entryElevator) return;

        LevelExitPoint temp = entryElevator;
        entryElevator = newPoint;
        exitElevator = temp;
    }

    void HandleExitActivation(LevelExitPoint exitPoint, bool decision)
    {
        SetNewEntryPoint(exitPoint);
        OnElevatorInteracted?.Invoke(exitPoint, decision);
    }
}