using PurrNet;
using PurrNet.Modules;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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

    public event Action<RoomState> OnStateChanged;
    public event Action<GameMap> OnMapChanged;
    [SerializeField] private UnityEvent<MapStateData> OnMapStateApplied;

    [SerializeField] private string[] mapCollection;
    [SerializeField, Range(0, 1)] private float anomalyChance = .5f;
    [SerializeField] private string[] punishmentRooms;
    [SerializeField] private string winRoom;

    private GameMap activeMap;
    private MapOrientor mapOrientor;
    private IndexPicker anomalyIndexPicker = new(1);
    private MapStateData currentMapState;
    private Scene loadedScene;

    // Serializes server-side transitions so one fully completes (scene load,
    // entry point update, broadcast) before the next one is allowed to start.
    private readonly SemaphoreSlim transitionLock = new(1, 1);

    // Holds state received (via RPC) whose target scene hasn't finished loading locally yet. 
    private MapStateData? pendingData;

    public RoomState CurrentState => currentMapState.roomState;
    public bool HasAnomaly => currentMapState.roomState == RoomState.AnomalyRoom;
    public MapOrientor MapOrientor => mapOrientor;
    public GameMap ActiveMap => activeMap;

    private void Awake()
    {
        mapOrientor = GetComponent<MapOrientor>();
        OnMapChanged += mapOrientor.OrientMap;
        GameMap.OnMapLoaded += HandleMapLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameMap.OnMapLoaded -= HandleMapLoaded;
    }

    #region Authority Methods
    public void DecideNextMapVariation() => DecideNextMapVariation(withAnomalies: UnityEngine.Random.value <= anomalyChance);
    public void DecideNextMapVariation(bool withAnomalies)
    {
        if (!isServer) return;

        _ = QueueTransition(state =>
        {
            state.variationIndex = withAnomalies ? anomalyIndexPicker.GetNext() : -1;
            state.roomState = withAnomalies ? RoomState.AnomalyRoom : RoomState.NormalRoom;
            return state;
        });
    }
    public void EnablePunishmentRoom_Server()
    {
        if (!isServer) return;

        _ = QueueTransition(state =>
        {
            state.uniqueRoomIndex = UnityEngine.Random.Range(0, punishmentRooms.Length);
            state.roomState = RoomState.PunishmentRoom;
            return state;
        });
    }
    public void EnableWinRoom_Server()
    {
        if (!isServer) return;

        _ = QueueTransition(state =>
        {
            state.roomState = RoomState.WinRoom;
            return state;
        });
    }
    public void PickMap_Server() => PickMapByIndex_Server(UnityEngine.Random.Range(0, mapCollection.Length));
    public void PickMapByIndex_Server(int index)
    {
        if (!isServer) return;
        index = Mathf.Clamp(index, 0, mapCollection.Length - 1);

        _ = QueueTransition(state =>
        {
            state.mapIndex = index;
            state.variationIndex = -1;
            state.roomState = RoomState.NormalRoom;
            return state;
        });
    }
    public void ChangeAnomalyChance(float percentage01) => anomalyChance = Mathf.Clamp01(percentage01);

    /// <summary>
    /// Queues a state mutation behind the transition lock. mutate is evaluated only once
    /// it's this transition's turn, so it always builds on top of the last committed state,
    /// not whatever currentMapState happened to be when the caller fired.
    /// </summary>
    private async Task QueueTransition(Func<MapStateData, MapStateData> mutate)
    {
        await transitionLock.WaitAsync();
        try
        {
            MapStateData nextState = mutate(currentMapState);
            await RegisterState(nextState);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            transitionLock.Release();
        }
    }

    private async Task RegisterState(MapStateData nextState)
    {
        await ResolveMapReferences(nextState);

        currentMapState = nextState;
        UpdateEntryPointData();
        BroadcastState_ObserversRpc(currentMapState);
    }

    private async Task ResolveMapReferences(MapStateData nextState)
    {
        switch (nextState.roomState)
        {
            case RoomState.NormalRoom:
            case RoomState.AnomalyRoom:
                await LoadMapAsync(mapCollection[nextState.mapIndex]);
                break;

            case RoomState.PunishmentRoom:
                await LoadMapAsync(punishmentRooms[nextState.uniqueRoomIndex]);
                break;

            case RoomState.WinRoom:
                await LoadMapAsync(winRoom);
                break;
        }
    }
    /// <summary>
    /// Returns the existing scene object, or instantiates a prefab copy if mapsArePrefabs.
    /// Server only.
    /// </summary>
    private async Task LoadMapAsync(string sceneName)
    {
        if (!isServer) return;

        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            if (loadedScene.name == sceneName) return;
            await networkManager.sceneModule.UnloadSceneAsync(loadedScene.buildIndex);
        }

        PurrSceneSettings settings = new()
        {
            isPublic = true,
            mode = LoadSceneMode.Additive
        };

        await networkManager.sceneModule.LoadSceneAsync(sceneName, settings);

        loadedScene = SceneManager.GetSceneByName(sceneName);
    }

    #endregion

    #region Sync Methods
    protected override void OnObserverAdded(PlayerID player)
    {
        base.OnObserverAdded(player);

        if (player.isServer) return;
        if (isServer && activeMap) BroadcastState_TargetRpc(player, currentMapState);
    }
    [ObserversRpc(runLocally: false)] private void BroadcastState_ObserversRpc(MapStateData data) => ApplyState(data);
    [TargetRpc] private void BroadcastState_TargetRpc(PlayerID player, MapStateData data) => ApplyState(data);
    #endregion

    #region Map Manipulation Methods
    private void ApplyState(MapStateData data)
    {
        // Clients sync their entry elevator transform.
        if (!isServer) mapOrientor.SyncEntryPointWith(data.entryPointID, data.entryPosition, data.entryRotation);

        currentMapState = data;
        OnStateChanged?.Invoke(data.roomState);
        OnMapStateApplied?.Invoke(data);

        if (IsMapReady(data))
        {
            pendingData = null;
            ApplyVisuals(data);
        }
        else pendingData = data;
    }

    private bool IsMapReady(MapStateData data)
    {
        string expectedScene = GetExpectedSceneName(data);
        return activeMap != null && expectedScene != null && activeMap.gameObject.scene.name == expectedScene;
    }

    private string GetExpectedSceneName(MapStateData data)
    {
        switch (data.roomState)
        {
            case RoomState.NormalRoom:
            case RoomState.AnomalyRoom:
            return mapCollection[data.mapIndex];

            case RoomState.PunishmentRoom: return punishmentRooms[data.uniqueRoomIndex];
            case RoomState.WinRoom: return winRoom;
            default: return null;
        }
    }

    private void ApplyVisuals(MapStateData data)
    {
        switch (data.roomState)
        {
            case RoomState.NormalRoom:
            case RoomState.AnomalyRoom:
                ShowMapVariation(data.variationIndex);
                break;

            default: OnMapChanged?.Invoke(activeMap); break;
        }
    }

    /// <summary>
    /// Makes the variation of an AnomalyMap visible. Negative values enable BaseMap.
    /// </summary>
    private void ShowMapVariation(int variationIndex)
    {
        if (activeMap == null || activeMap is not AnomalyMap anomalyMap)
        {
            Debug.LogWarning($"ShowMapVariation: active map is not an AnomalyMap!");
            return;
        }

        anomalyMap.DisableAll();

        if (variationIndex < 0)
        {
            anomalyMap.BaseMap.SetActive(true);
            OnMapChanged?.Invoke(activeMap);
            return;
        }

        AnomalyGroup variation = anomalyMap.GetAnomalyGroupAtIndex(variationIndex);
        if (!variation.GroupRoot)
        {
            Debug.LogWarning($"ShowMapVariation: no GroupRoot on variation {variationIndex} of {anomalyMap.BaseMap.name}");
            return;
        }

        anomalyMap.BaseMap.SetActive(!variation.ReplacesBaseMap);
        variation.GroupRoot.SetActive(true);
        OnMapChanged?.Invoke(activeMap);

        if (variation.AlmanacEntry != null) AlmanacDiscovery.Discover(variation.AlmanacEntry);
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

    private void HandleMapLoaded(GameMap map)
    {
        activeMap = map;

        if (map is AnomalyMap anomalyMap)
        {
            if (anomalyIndexPicker.Length != anomalyMap.AnomalyVariations.Count)
                anomalyIndexPicker.Reset(anomalyMap.AnomalyVariations.Count);
        }

        AlmanacDiscovery.Discover(map.AlmanacEntry);

        if (pendingData.HasValue && IsMapReady(pendingData.Value))
        {
            MapStateData state = pendingData.Value;
            pendingData = null;
            ApplyVisuals(state);
        }
    }
    #endregion
}