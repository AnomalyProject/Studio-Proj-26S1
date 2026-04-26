using PurrNet;
using UnityEngine;

[RequireComponent(typeof(MapOrientor))]
public class AnomalyManager : NetworkBehaviour
{
    public enum RoomState 
    { 
        NormalRoom,
        AnomalyRoom, 
        PunishmentRoom, 
        WinRoom 
    }

    struct MapStateData
    {
        // Map Data
        public int mapIndex;
        public int anomalyIndex;
        public RoomState roomState;
        public int uniqueRoomIndex;

        // Entry Point data.
        public NetworkID entryPointID;
        public Vector3 entryPosition;
        public Quaternion entryRotation;
    }

    public event System.Action<RoomState> OnStateChanged;
    public event System.Action<GameMap> OnMapChanged;

    [SerializeField] AnomalyMap[] mapCollection;
    [SerializeField, Range(0,1)] float anomalyChance = .5f;
    [SerializeField] GameMap[] punishmentRooms;
    [SerializeField] GameMap winRoom;
    [SerializeField, Tooltip("Weather to Instantiate Map Prefabs or Enable/Disable Maps from the scene.")] bool mapsArePrefabs = true;

    AnomalyMap activeMap;
    GameMap activeUniqueRoom;
    MapOrientor mapOrientor;
    MapStateData currentMapState;
    public RoomState CurrentState => currentMapState.roomState;
    public bool HasAnomaly => currentMapState.roomState == RoomState.AnomalyRoom;
    public MapOrientor MapOrientor => mapOrientor;

    void Awake()
    {
        mapOrientor = GetComponent<MapOrientor>();
        OnMapChanged += mapOrientor.OrientMap;

        if (!mapsArePrefabs)
        {
            foreach (var map in mapCollection) map.DisableAll();
            foreach (var punish in punishmentRooms) punish.gameObject.SetActive(false);
            winRoom.gameObject.SetActive(false);
        }
    }

    #region Sync Methods
    protected override void OnObserverAdded(PlayerID player)
    {
        base.OnObserverAdded(player);

        if (isServer) SyncMapWithData_TargetRpc(player, currentMapState);
    }
    private void SyncMapWithData(MapStateData data)
    {
        // Sync entry elevator position, mainly for late joiners
        if(!isServer) mapOrientor.SyncEntryPointWith(data.entryPointID, data.entryPosition, data.entryRotation);

        // Handle Map Desync
        bool mapIsNull = activeMap == null;
        bool desyncedMap = currentMapState.mapIndex != data.mapIndex;
        if (mapIsNull || desyncedMap) TryLoadMap(mapIndex: data.mapIndex);

        switch (data.roomState)
        {
            case RoomState.NormalRoom:
            case RoomState.AnomalyRoom:
                ApplyMapVariation(data.anomalyIndex); break;

            case RoomState.PunishmentRoom: EnablePunishmentRoom(data.uniqueRoomIndex); break;
            case RoomState.WinRoom: EnableWinRoom(); break;
        }

        currentMapState = data;
        OnStateChanged?.Invoke(data.roomState);
    }
    [ObserversRpc] private void SyncMapWithData_ObserversRpc(MapStateData data) => SyncMapWithData(data);
    [TargetRpc] private void SyncMapWithData_TargetRpc(PlayerID player, MapStateData data) => SyncMapWithData(data);
    private void RegisterState(RoomState newState)
    {
        if (!isServer) return;

        currentMapState.roomState = newState;
        UpdateEntryPointData();
        SyncMapWithData_ObserversRpc(currentMapState);
    }
    #endregion

    #region Authority Methods
    /// <summary>
    /// Randomly decides the next map variation based on the <see cref="anomalyChance"/>.
    /// </summary>
    public void DecideNextMapVariation() => DecideNextMapVariation(withAnomalies: Random.value <= anomalyChance);
    /// <summary>
    /// Changes the map variation based on the given parameter. 
    /// If <paramref name="withAnomalies"/> is false, it will simply enable the base map and disable any active anomaly variations. 
    /// If true, it will enable a random anomaly variation and disable the base map if the map is set to have whole room variations.
    /// </summary>
    /// <param name="withAnomalies"></param>
    public void DecideNextMapVariation(bool withAnomalies)
    {
        if (!isServer) return;

        if (activeMap == null)
        {
            Debug.LogError("Tried to decide on map variation but no active map!");
            return;
        }

        if (!withAnomalies)
        {
            currentMapState.anomalyIndex = -1;
            RegisterState(RoomState.NormalRoom);
            return;
        }

        currentMapState.anomalyIndex = activeMap.GetRandomUnusedAnomalyIndex();
        RegisterState(RoomState.AnomalyRoom);
    }
    /// <summary>
    /// Enables a random punishment room from the <see cref="punishmentRooms"/>, disabling the active map and any active anomaly variations.
    /// </summary>
    public void EnablePunishmentRoom_Server()
    {
        if (!isServer) return;
        currentMapState.uniqueRoomIndex = Random.Range(0, punishmentRooms.Length);
        RegisterState(RoomState.PunishmentRoom);
    }
    /// <summary>
    /// Enables the win room, disabling the active map and any active anomaly variations or punishment rooms.
    /// </summary>
    public void EnableWinRoom_Server()
    {
        if (!isServer) return;
        RegisterState(RoomState.WinRoom);
    }
    /// <summary>
    /// Picks a random map from the <see cref="mapCollection"/> and sets it as the active map.
    /// </summary>
    /// <returns>True if successful, otherwirse False.</returns>
    public void PickMap_Server()
    {
        if (!isServer) return;
        currentMapState.mapIndex = Random.Range(0, mapCollection.Length);
        currentMapState.anomalyIndex = -1;
        RegisterState(RoomState.NormalRoom);
    }
    #endregion

    #region Map Manipulation Methods
    private void ApplyMapVariation(int variationIndex)
    {
        ClearActiveState(destroyActiveMap: false);

        if (variationIndex < 0)
        {
            activeMap.BaseMap.SetActive(true);
            OnMapChanged?.Invoke(activeMap);
            return;
        }

        AnomalyGroup nextVariation = activeMap.GetAnomalyGroupAtIndex(variationIndex);

        if (!nextVariation.GroupRoot)
        {
            Debug.LogWarning($"Failed to get next anomaly variation. Check if the active map ({activeMap.BaseMap.name}) has any variations assigned.");
            return;
        }

        activeMap.BaseMap.SetActive(!nextVariation.ReplacesBaseMap);
        nextVariation.GroupRoot.SetActive(true);
        OnMapChanged?.Invoke(activeMap);
    }
    private void EnablePunishmentRoom(int atIndex)
    {
        if (punishmentRooms.Length == 0)
        {
            Debug.LogWarning("Tried to enable punishment room but there are no punishment rooms in the array.");
            return;
        }

        ClearActiveState(destroyActiveMap: false);

        GameMap map = punishmentRooms[atIndex];
        activeUniqueRoom = CreateMap(map);

        if (activeUniqueRoom == null)
        {
            Debug.LogWarning($"Tried to enable punishment room at index {atIndex} but it is null.");
            return;
        }

        currentMapState.uniqueRoomIndex = atIndex;
        OnMapChanged?.Invoke(activeUniqueRoom);
    }

    /// <summary>
    /// Picks the map at the given index from the <see cref="mapCollection"/> and sets it as the active map.
    /// </summary>
    /// <returns>True if successful, otherwirse False.</returns>
    /// <param name="mapIndex"></param>
    private bool TryLoadMap(int mapIndex)
    {
        if (mapCollection.Length == 0)
        {
            Debug.LogWarning("Tried to pick random map but there are no maps in the collection.");
            return false;
        }

        if (mapIndex < 0 || mapIndex >= mapCollection.Length)
        {
            Debug.LogWarning($"Tried to pick map at index {mapIndex} but it is out of bounds for the map collection.");
            return false;
        }

        ClearActiveState(destroyActiveMap: mapsArePrefabs);

        if (!mapCollection[mapIndex])
        {
            Debug.LogWarning($"Tried to pick map at index {mapIndex} but it is null.");
            return false;
        }

        AnomalyMap map = mapCollection[mapIndex];
        activeMap = CreateMap(map);

        activeMap.DisableAll(keepBase: true);
        OnMapChanged?.Invoke(activeMap);
        return true;
    }
    private void EnableWinRoom()
    {
        if (!winRoom)
        {
            Debug.LogWarning("Tried to enable win room but there is no win room assigned.");
            return;
        }

        ClearActiveState(false);
        activeUniqueRoom = CreateMap(winRoom);
        OnMapChanged?.Invoke(activeUniqueRoom);
    }
    private void ClearActiveState(bool destroyActiveMap)
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

        ReleaseMap(ref activeUniqueRoom);
    }
    #endregion

    #region Helpers & Utils
    /// <summary>
    /// Modify the chance of anomalous room variations. The value is clamped between 0 and 1.
    /// </summary>
    /// <param name="percentage01"></param>
    public void ChangeAnomalyChance(float percentage01) => anomalyChance = Mathf.Clamp01(percentage01);
    private TMap CreateMap<TMap>(TMap map) where TMap : GameMap
    {
        TMap result;

        if (mapsArePrefabs) result = Instantiate(map);
        else result = map;

        result.gameObject.SetActive(true);
        return result;
    }
    private void ReleaseMap(ref GameMap map)
    {
        if (!map) return;

        if(mapsArePrefabs) Destroy(map.gameObject);
        else map.gameObject.SetActive(false);
        map = null;
    }
    private void UpdateEntryPointData()
    {
        LevelExitPoint entryElevator = mapOrientor.EntryElevator;
        currentMapState.entryPointID = entryElevator.id.Value;
        currentMapState.entryPosition = entryElevator.transform.position;
        currentMapState.entryRotation = entryElevator.transform.rotation;
    }
    #endregion
}