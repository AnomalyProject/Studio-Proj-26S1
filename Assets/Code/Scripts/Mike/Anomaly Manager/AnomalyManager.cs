using UnityEngine;

[RequireComponent(typeof(MapOrientor))]
public class AnomalyManager : MonoBehaviour
{
    public enum RoomState 
    { 
        NormalRoom,
        AnomalyRoom, 
        PunishmentRoom, 
        WinRoom 
    }

    public event System.Action<RoomState> OnStateChanged;
    public event System.Action<GameMap> OnMapChanged;

    [SerializeField] AnomalyMap[] mapCollection;
    [SerializeField, Range(0,1)] float anomalyChance = .5f;
    [SerializeField] GameMap[] punishmentRooms;
    [SerializeField] GameMap winRoom;
    [SerializeField, Tooltip("Weather to Instantiate Map Prefabs or Enable/Disable Maps from the scene.")] bool mapsArePrefabs = true;

    AnomalyMap activeMap;
    GameMap activePunishmentRoom, activeWinRoom;
    GameObject activeAnomalyGroup;
    RoomState currentState;
    MapOrientor mapOrientor;

    public RoomState CurrentState => currentState;
    public bool HasAnomaly => currentState == RoomState.AnomalyRoom;
    public MapOrientor MapOrientor => mapOrientor;

    void Awake()
    {
        mapOrientor = GetComponent<MapOrientor>();
        OnMapChanged += mapOrientor.OrientMap;

        if(!mapsArePrefabs)
        foreach (var map in mapCollection) map.DisableAll();
    }

    /// <summary>
    /// Modify the chance of anomalous room variations. The value is clamped between 0 and 1.
    /// </summary>
    /// <param name="percentage01"></param>
    public void ChangeAnomalyChance(float percentage01) => anomalyChance = Mathf.Clamp01(percentage01);

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
        if (activeMap == null && !TryPickMap()) return;

        ClearActiveState(destroyActiveMap: false);

        if (!withAnomalies)
        {
            activeMap.BaseMap.SetActive(true);
            ChangeState(RoomState.NormalRoom);
            OnMapChanged?.Invoke(activeMap);
            return;
        }

        AnomalyGroup nextVariation = activeMap.GetNextAnomalyGroup();

        if (!nextVariation.GroupRoot)
        {
            Debug.LogWarning($"Failed to get next anomaly variation. Check if the active map ({activeMap.BaseMap.name}) has any variations assigned.");
            return;
        }

        activeAnomalyGroup = nextVariation.GroupRoot;
        activeMap.BaseMap.SetActive(!nextVariation.ReplacesBaseMap);
        activeAnomalyGroup.SetActive(true);
        OnMapChanged?.Invoke(activeMap);
        ChangeState(RoomState.AnomalyRoom);
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

        ClearActiveState(destroyActiveMap: false);

        int roomIndex = Random.Range(0, punishmentRooms.Length);
        GameMap map = punishmentRooms[roomIndex];
        activePunishmentRoom = CreateMap(map);

        if(activePunishmentRoom == null)
        {
            Debug.LogWarning($"Tried to enable punishment room at index {roomIndex} but it is null.");
            return;
        }

        OnMapChanged?.Invoke(activePunishmentRoom);
        ChangeState(RoomState.PunishmentRoom);
    }

    /// <summary>
    /// Picks a random map from the <see cref="mapCollection"/> and sets it as the active map.
    /// </summary>
    /// <returns>True if successful, otherwirse False.</returns>
    public bool TryPickMap() => TryPickMap(Random.Range(0, mapCollection.Length));
    public void PickMap() => TryPickMap(); // just for the inspector lol

    /// <summary>
    /// Picks the map at the given index from the <see cref="mapCollection"/> and sets it as the active map.
    /// </summary>
    /// <returns>True if successful, otherwirse False.</returns>
    /// <param name="mapIndex"></param>
    public bool TryPickMap(int mapIndex)
    {
        if (mapCollection.Length == 0)
        {
            Debug.LogWarning("Tried to pick random map but there are no maps in the collection.");
            return false;
        }

        if(mapIndex < 0 || mapIndex >= mapCollection.Length)
        {
            Debug.LogWarning($"Tried to pick map at index {mapIndex} but it is out of bounds for the map collection.");
            return false;
        }

        ClearActiveState(destroyActiveMap: mapsArePrefabs);

        if(!mapCollection[mapIndex])
        {
            Debug.LogWarning($"Tried to pick map at index {mapIndex} but it is null.");
            return false;
        }

        AnomalyMap map = mapCollection[mapIndex];
        activeMap = CreateMap(map) as AnomalyMap;

        activeMap.DisableAll(keepBase: true);
        ChangeState(RoomState.NormalRoom);
        OnMapChanged?.Invoke(activeMap);
        return true;
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

        ClearActiveState(false);
        activeWinRoom = CreateMap(winRoom);
        OnMapChanged?.Invoke(activeWinRoom);
        ChangeState(RoomState.WinRoom);
    }
    void ClearActiveState(bool destroyActiveMap)
    {
        if (activeMap)
        {
            if (destroyActiveMap)
            {
                Destroy(activeMap.gameObject);
                activeMap = null;
            }
            else activeMap.DisableAll();
        }

        activeAnomalyGroup = null;

        ReleaseMap(ref activePunishmentRoom);
        ReleaseMap(ref activeWinRoom);
    }
    void ChangeState(RoomState newState)
    {
        if(currentState == newState) return;

        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }
    GameMap CreateMap(GameMap map)
    {
        GameMap result;

        if (mapsArePrefabs) result = Instantiate(map);
        else result = map;

        result.gameObject.SetActive(true);
        return result;
    }
    void ReleaseMap(ref GameMap map)
    {
        if (!map) return;

        if(mapsArePrefabs) Destroy(map.gameObject);
        else map.gameObject.SetActive(false);
        map = null;
    }
}