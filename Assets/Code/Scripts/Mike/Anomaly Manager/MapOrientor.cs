using System;
using UnityEngine;

public class MapOrientor : MonoBehaviour
{
    [SerializeField] LevelExitPoint entryElevator, exitElevator;
    [SerializeField, Tooltip("If interacting with the entry exit means there is an anomaly in the room")] bool entryHasAnomaly;
    public event Action<bool> OnElevatorInteracted;

    void Awake()
    {
        entryElevator.OnExitActivated += HandleExitPoint;
        exitElevator.OnExitActivated += HandleExitPoint;

        SetEntryPoint(entryElevator);
    }

    /// <summary>
    /// Change provided map's orientation to match the elevators.
    /// </summary>
    /// <param name="map"></param>
    public void OrientMap(GameMap map)
    {
        // Entry Point is the parent of the whole map, configured in GameMap's awake.
        map.EntryPointAnchor.position = entryElevator.transform.position;
        map.EntryPointAnchor.rotation = entryElevator.transform.rotation;

        exitElevator.transform.position = map.ExitPointAnchor.position;
        exitElevator.transform.rotation = map.ExitPointAnchor.rotation;
    }
    void SetEntryPoint(LevelExitPoint newPoint)
    {
        if (newPoint != entryElevator)
        {
            var temp = entryElevator;
            entryElevator = newPoint;
            exitElevator = temp;
        }

        entryElevator.SetChoice(entryHasAnomaly);
        exitElevator.SetChoice(!entryHasAnomaly);
    }

    void HandleExitPoint(LevelExitPoint exitPoint, bool decision)
    {
        SetEntryPoint(exitPoint);
        OnElevatorInteracted?.Invoke(decision);
    }
}