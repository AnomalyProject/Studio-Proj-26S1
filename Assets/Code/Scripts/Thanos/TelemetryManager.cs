using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine.UnityConsent;

public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager Instance { get; private set; }
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void InitializeWithPlayerName(string displayName)
    {
        if (isInitialized) return;

        try
        {
            UnityServices.ExternalUserId = displayName;

            await UnityServices.InitializeAsync();
            EndUserConsent.SetConsentState(new ConsentState { AnalyticsIntent = ConsentStatus.Granted });

            isInitialized = true;
            Debug.Log($"[Telemetry] Analytics initialized for local player: {displayName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Telemetry] Failed initializing: {e.Message}");
        }
    }

    public void TrackFloorCompletion(int floorNumber, bool roomHasAnomaly, string playerChoice, float timeSpent, string anomalyName)
    {
        if (!isInitialized || UnityServices.State != ServicesInitializationState.Initialized) return;

        bool choseEntry = playerChoice == "EntryElevator";
        bool isChoiceCorrect = (roomHasAnomaly && choseEntry) || (!roomHasAnomaly && !choseEntry);

        CustomEvent floorEvent = new CustomEvent("floor_completed")
        {
            { "floor_number", floorNumber },
            { "room_had_anomaly", roomHasAnomaly },
            { "anomaly_id_name", anomalyName },
            { "player_choice", playerChoice },
            { "is_choice_correct", isChoiceCorrect },
            { "time_spent_seconds", timeSpent }
        };

        AnalyticsService.Instance.RecordEvent(floorEvent);
        AnalyticsService.Instance.Flush();
    }
}