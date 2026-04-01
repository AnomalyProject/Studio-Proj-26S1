using UnityEngine;

public class AnomalyManager : MonoBehaviour
{
    public event System.Action OnAnomalyMapChanged, OnPunishmentRoomActivation;

    [SerializeField] bool keepBaseMapOnAnomaly = true;
    [SerializeField] AnomalyMap[] mapCollection;
    [SerializeField, Range(0,100)] int anomalyChance = 50;
    [SerializeField] GameObject[] punishmentRooms;

    AnomalyMap activeMap;
    GameObject activeAnomalyVariation, activePunishmentRoom;
    public bool HasAnomaly => activeAnomalyVariation != null;

    void Awake() => PickRandomMap();
    public void DecideNextMapVariation()
    {
        if (activeMap == null) PickRandomMap();

        if (activeAnomalyVariation != null)
        {
            activeAnomalyVariation.SetActive(false);
            activeAnomalyVariation = null;
        }

        bool anomalyRound = Random.Range(0, 101) <= anomalyChance;

        if (!anomalyRound)
        {
            activeMap.BaseMap.SetActive(true);
            OnAnomalyMapChanged?.Invoke();
            return;
        }

        activeMap.BaseMap.SetActive(keepBaseMapOnAnomaly);

        int variationIndex = Random.Range(0, activeMap.AnomalyVariations.Length);
        activeAnomalyVariation = activeMap.AnomalyVariations[variationIndex];
        activeAnomalyVariation.SetActive(true);
        OnAnomalyMapChanged?.Invoke();
    }
    public void DisableActiveMap()
    {
        if(activeMap == null) return;
        activeAnomalyVariation?.SetActive(false);
        activeMap.BaseMap?.SetActive(false);
    }
    public void EnablePunishmentRoom()
    {
        if(punishmentRooms.Length == 0)
        {
            Debug.LogWarning("Tried to enable punishment room but there are no punishment rooms in the array.");
            return;
        }

        DisableActiveMap();
        if(activePunishmentRoom) activePunishmentRoom.SetActive(false);
        int punishmentRoomIndex = Random.Range(0, punishmentRooms.Length);
        activePunishmentRoom = punishmentRooms[punishmentRoomIndex];
        activePunishmentRoom?.SetActive(true);
        OnPunishmentRoomActivation?.Invoke();
    }
    public void DisablePunishmentRoom()
    {
        if(activePunishmentRoom == null)
        {
            Debug.LogWarning("Tried to disable punishment room but there is no active punishment room.");
            return;
        }

        activePunishmentRoom.SetActive(false);
        DecideNextMapVariation();
    }
    void PickRandomMap()
    {
        if(activeMap != null)
        {
            activeAnomalyVariation?.SetActive(false);
            activeAnomalyVariation = null;
            activeMap.BaseMap.SetActive(false);
        }

        int mapIndex = Random.Range(0, mapCollection.Length);
        activeMap = mapCollection[mapIndex];
        activeMap.BaseMap.SetActive(true);
    }
}