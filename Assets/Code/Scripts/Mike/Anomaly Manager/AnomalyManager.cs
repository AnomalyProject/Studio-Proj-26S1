using UnityEngine.Events;
using UnityEngine;
using PurrNet;

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
    public struct MapStateData
    {
        public int mapIndex;
        public int variationIndex;
        public RoomState roomState;
        public int uniqueRoomIndex;

        public NetworkID entryPointID;
        public Vector3 entryPosition;
        public Quaternion entryRotation;
    }

    public event System.Action<RoomState> OnStateChanged;
    public event System.Action<GameMap> OnMapChanged;
    [SerializeField] private UnityEvent<MapStateData> OnMapStateApplied;

    [SerializeField] private AnomalyMap[] mapCollection;
    [SerializeField, Range(0, 1)] private float anomalyChance = .5f;
    [SerializeField] private GameMap[] punishmentRooms;
    [SerializeField] private GameMap winRoom;
    [SerializeField, Tooltip("Whether to Instantiate Map Prefabs or Enable/Disable Maps from the scene.")] private bool mapsArePrefabs = true;

    private int loadedMapIndex = -1;
    private AnomalyMap activeMap;
    private GameMap activeUniqueRoom;
    private MapOrientor mapOrientor;
    private MapStateData currentMapState;

    public RoomState CurrentState => currentMapState.roomState;
    public bool HasAnomaly => currentMapState.roomState == RoomState.AnomalyRoom;
    public MapOrientor MapOrientor => mapOrientor;

    private void Awake()
    {
        mapOrientor = GetComponent<MapOrientor>();
        OnMapChanged += HandleMapChange;

        if (!mapsArePrefabs)
        {
            foreach (var map in mapCollection) map.DisableAll();
            foreach (var punish in punishmentRooms) punish.gameObject.SetActive(false);
            if (winRoom) winRoom.gameObject.SetActive(false);
        }
    }

    #region Authority Methods
    public void DecideNextMapVariation() => DecideNextMapVariation(withAnomalies: Random.value <= anomalyChance);
    public void DecideNextMapVariation(bool withAnomalies)
    {
        if (!isServer) return;
        if (activeMap == null) { Debug.LogError("No active map to apply variation to!"); return; }

        currentMapState.variationIndex = withAnomalies && activeMap.HasAnomalyVariations? activeMap.GetRandomUnusedAnomalyIndex() : -1;
        RegisterState(withAnomalies? RoomState.AnomalyRoom : RoomState.NormalRoom);
    }
    public void EnablePunishmentRoom_Server()
    {
        if (!isServer) return;
        currentMapState.uniqueRoomIndex = Random.Range(0, punishmentRooms.Length);
        RegisterState(RoomState.PunishmentRoom);
    }
    public void EnableWinRoom_Server()
    {
        if (!isServer) return;
        RegisterState(RoomState.WinRoom);
    }
    public void PickMap_Server() => PickMapByIndex_Server(Random.Range(0, mapCollection.Length));
    public void PickMapByIndex_Server(int index)
    {
        if (!isServer) return;
        index = Mathf.Clamp(index, 0, mapCollection.Length);

        currentMapState.mapIndex = index;
        currentMapState.variationIndex = -1;
        RegisterState(RoomState.NormalRoom);
    }
    public void ChangeAnomalyChance(float percentage01) => anomalyChance = Mathf.Clamp01(percentage01);

    /// <summary>
    /// Server: resolves which map objects are needed for the new state, then broadcasts state to all clients.
    /// </summary>
    private void RegisterState(RoomState newState)
    {
        if (!isServer) return;

        currentMapState.roomState = newState;

        ResolveMapReferences(newState);
        UpdateEntryPointData();
        BroadcastState_ObserversRpc(activeMap, activeUniqueRoom, currentMapState);
    }
    /// <summary>
    /// Server-side only. Instantiates or picks the correct map objects for the given state,
    /// releasing whatever was active before.
    /// </summary>
    private void ResolveMapReferences(RoomState newState)
    {
        ReleaseUniqueRoom();

        switch (newState)
        {
            case RoomState.NormalRoom:
            case RoomState.AnomalyRoom:
                // Only reload the AnomalyMap if it changed.
                if (activeMap == null || currentMapState.mapIndex != loadedMapIndex)
                {
                    ReleaseActiveMap(destroy: mapsArePrefabs);
                    activeMap = SpawnOrGet(mapCollection[currentMapState.mapIndex]);
                    loadedMapIndex = currentMapState.mapIndex;
                }
                break;

            case RoomState.PunishmentRoom:
                activeUniqueRoom = SpawnOrGet(punishmentRooms[currentMapState.uniqueRoomIndex]);
                break;

            case RoomState.WinRoom:
                activeUniqueRoom = SpawnOrGet(winRoom);
                break;
        }
    }
    /// <summary>
    /// Returns the existing scene object, or instantiates a prefab copy if mapsArePrefabs.
    /// Server only.
    /// </summary>
    private TMap SpawnOrGet<TMap>(TMap source) where TMap : GameMap
    {
        if (!isServer) return null;
        TMap result = mapsArePrefabs ? Instantiate(source) : source;
        return result;
    }
    #endregion

    #region Sync Methods
    protected override void OnObserverAdded(PlayerID player)
    {
        base.OnObserverAdded(player);

        if (isServer) BroadcastState_TargetRpc(player, activeMap, activeUniqueRoom, currentMapState);
    }
    [ObserversRpc(runLocally: false)] private void BroadcastState_ObserversRpc(AnomalyMap map, GameMap uniqueRoom, MapStateData data) => ApplyState(map, uniqueRoom, data);
    [TargetRpc] private void BroadcastState_TargetRpc(PlayerID player, AnomalyMap map, GameMap uniqueRoom, MapStateData data) => ApplyState(map, uniqueRoom, data);
    #endregion

    #region Map Manipulation Methods
    /// <summary>
    /// The single method that actually changes what's visible.
    /// Runs on both server (directly) and clients (via RPC).
    /// On clients, map/uniqueRoom are the network-resolved prefab instances.
    /// On the server, they're the same references that were just spawned.
    /// </summary>
    private void ApplyState(AnomalyMap map, GameMap uniqueRoom, MapStateData data)
    {
        // Clients sync their entry elevator transform.
        if (!isServer) mapOrientor.SyncEntryPointWith(data.entryPointID, data.entryPosition, data.entryRotation);

        HideAll(); // Clean previous map.

        activeMap = map;
        activeUniqueRoom = uniqueRoom;

        switch (data.roomState) // Then activate what needs be.
        {
            case RoomState.NormalRoom:
            case RoomState.AnomalyRoom:
                activeMap.gameObject.SetActive(true);
                ShowMapVariation(data.variationIndex);
                break;

            case RoomState.PunishmentRoom:
            case RoomState.WinRoom:
                activeUniqueRoom.gameObject.SetActive(true);
                OnMapChanged?.Invoke(activeUniqueRoom);
                break;
        }

        currentMapState = data;
        OnStateChanged?.Invoke(data.roomState);
        OnMapStateApplied?.Invoke(data);
    }
    /// <summary>
    /// Hides active map and unique room without destroying them.
    /// </summary>
    private void HideAll()
    {
        if (activeMap)
        {
            activeMap.DisableAll();
            activeMap.gameObject.SetActive(false);
        }
        if (activeUniqueRoom) activeUniqueRoom.gameObject.SetActive(false);
    }
    /// <summary>
    /// Makes the variation of an AnomalyMap visible. Negative values enable BaseMap.
    /// </summary>
    private void ShowMapVariation(int variationIndex)
    {
        if (variationIndex < 0)
        {
            activeMap.BaseMap.SetActive(true);
            OnMapChanged?.Invoke(activeMap);
            return;
        }

        AnomalyGroup variation = activeMap.GetAnomalyGroupAtIndex(variationIndex);
        if (!variation.GroupRoot)
        {
            Debug.LogWarning($"ShowMapVariation: no GroupRoot on variation {variationIndex} of {activeMap.BaseMap.name}");
            return;
        }

        activeMap.BaseMap.SetActive(!variation.ReplacesBaseMap);
        variation.GroupRoot.SetActive(true);
        OnMapChanged?.Invoke(activeMap);

        if (variation.AlmanacEntry != null) AlmanacDiscovery.Discover(variation.AlmanacEntry);
    }
    private void ReleaseActiveMap(bool destroy)
    {
        if (!activeMap) return;
        if (destroy && isServer) Destroy(activeMap.gameObject);
        else activeMap.DisableAll();
        activeMap = null;
    }
    private void ReleaseUniqueRoom()
    {
        if (!activeUniqueRoom) return;
        if (mapsArePrefabs && isServer) Destroy(activeUniqueRoom.gameObject);
        else activeUniqueRoom.gameObject.SetActive(false);
        activeUniqueRoom = null;
    }
    #endregion

    #region Helper Methods
    private void UpdateEntryPointData()
    {
        LevelExitPoint entry = mapOrientor.EntryElevator;
        currentMapState.entryPointID = entry.id.Value;
        currentMapState.entryPosition = entry.transform.position;
        currentMapState.entryRotation = entry.transform.rotation;
    }
    private void HandleMapChange(GameMap map)
    {
        mapOrientor.OrientMap(map);
        AlmanacDiscovery.Discover(map.AlmanacEntry);
    }
    #endregion
}