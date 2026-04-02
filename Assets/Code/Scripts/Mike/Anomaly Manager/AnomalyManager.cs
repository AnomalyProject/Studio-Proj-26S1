using UnityEngine;

public class AnomalyManager : MonoBehaviour
{
    public event System.Action OnAnomalyMapChanged, OnPunishmentRoomActivation, OnWinRoomActivation;

    [SerializeField] AnomalyMap[] mapCollection;
    [SerializeField, Range(0,1)] float anomalyChance = .5f;
    [SerializeField] GameObject[] punishmentRooms;
    [SerializeField] GameObject winRoom;
    [SerializeField] bool pickMapOnAwake = true;

    AnomalyMap activeMap;
    GameObject activeAnomalyVariation, activePunishmentRoom;
    public bool HasAnomaly => activeAnomalyVariation != null && activeAnomalyVariation.activeInHierarchy;

    void Awake()
    {
        foreach(var map in mapCollection) map.DisableAll();
        if (pickMapOnAwake) PickMap();
    }

    /// <summary>
    /// Randomly decides the next map variation based on the <see cref="anomalyChance"/>.
    /// </summary>
    public void DecideNextMapVariation() => DecideNextMapVariation(Random.value <= anomalyChance);

    /// <summary>
    /// Changes the map variation based on the given parameter. 
    /// If <paramref name="withAnomalies"/> is false, it will simply enable the base map and disable any active anomaly variations. 
    /// If true, it will enable a random anomaly variation and disable the base map if the map is set to have whole room variations.
    /// </summary>
    /// <param name="withAnomalies"></param>
    public void DecideNextMapVariation(bool withAnomalies)
    {
        if (activeMap == null) PickMap();

        ClearActiveState();

        if (!withAnomalies)
        {
            activeMap.BaseMap.SetActive(true);
            OnAnomalyMapChanged?.Invoke();
            return;
        }

        activeMap.BaseMap.SetActive(!activeMap.WholeRoomVariations);

        GameObject nextVariation = activeMap.GetNextAnomalyGroup();

        if (!nextVariation)
        {
            Debug.LogWarning($"Failed to get next anomaly variation. Check if the active map ({activeMap.BaseMap.name}) has any variations assigned.");
            return;
        }

        activeAnomalyVariation = nextVariation;
        activeAnomalyVariation.SetActive(true);
        OnAnomalyMapChanged?.Invoke();
    }

    /// <summary>
    /// Enables a random punishment room from the <see cref="punishmentRooms"/>, disabling the active map and any active anomaly variations.
    /// </summary>
    public void EnablePunishmentRoom()
    {
        if(punishmentRooms.Length == 0)
        {
            Debug.LogWarning("Tried to enable punishment room but there are no punishment rooms in the array.");
            return;
        }

        ClearActiveState();

        int punishmentRoomIndex = Random.Range(0, punishmentRooms.Length);
        activePunishmentRoom = punishmentRooms[punishmentRoomIndex];
        activePunishmentRoom?.SetActive(true);
        OnPunishmentRoomActivation?.Invoke();
    }

    /// <summary>
    /// Picks a random map from the <see cref="mapCollection"/> and sets it as the active map.
    /// </summary>
    public void PickMap() => PickMap(Random.Range(0, mapCollection.Length));

    /// <summary>
    /// Picks the map at the given index from the <see cref="mapCollection"/> and sets it as the active map.
    /// </summary>
    /// <param name="mapIndex"></param>
    public void PickMap(int mapIndex)
    {
        if (mapCollection.Length == 0)
        {
            Debug.LogWarning("Tried to pick random map but there are no maps in the collection.");
            return;
        }

        if(mapIndex < 0 || mapIndex >= mapCollection.Length)
        {
            Debug.LogWarning($"Tried to pick map at index {mapIndex} but it is out of bounds for the map collection.");
            return;
        }

        ClearActiveState();

        activeMap = mapCollection[mapIndex];
        activeMap.BaseMap.SetActive(true);
        OnAnomalyMapChanged?.Invoke();
    }

    /// <summary>
    /// Enables the win room, disabling the active map and any active anomaly variations or punishment rooms.
    /// </summary>
    public void EnableWinRoom()
    {
        if (!winRoom)
        {
            Debug.LogWarning("Tried to enable win room but there is no win room assigned.");
            return;
        }

        ClearActiveState();

        winRoom?.SetActive(true);
        OnWinRoomActivation?.Invoke();
    }
    
    void ClearActiveState()
    {
        if (activeAnomalyVariation)
        {
            activeAnomalyVariation.SetActive(false);
            activeAnomalyVariation = null;
        }

        if (activePunishmentRoom)
        {
            activePunishmentRoom.SetActive(false);
            activePunishmentRoom = null;
        }

        if(winRoom)
        winRoom.SetActive(false);

        if(activeMap)
        activeMap.DisableAll();
    }
}