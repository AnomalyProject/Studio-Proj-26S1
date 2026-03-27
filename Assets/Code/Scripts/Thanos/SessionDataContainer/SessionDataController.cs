using UnityEngine;
using Newtonsoft.Json;

public class SessionController : MonoBehaviour
{
    [Header("Session State")]
    public SessionData CurrentSession;
    
    [Header("Debug Settings")]
    [SerializeField] private ulong testSteamId = 676967690;
    [SerializeField] private string testName = "Xx|Obama_killer69|xX";

    void Start()
    {
        RunProductionDemo();
    }
    public void RunProductionDemo()
    {
        Debug.Log("<color=cyan>=== Starting Session Demo ===</color>");

        CurrentSession = new SessionData
        {
            HostSteamID = 76561197960287930,
            MapName = "Industrial Plant",
            GameMode = "Extract",
            MaxPlayers = 2
        };

        var host = new PlayerSessionInfo(CurrentSession.HostSteamID, "Host_Alpha", true);
        CurrentSession.AddPlayer(host);

        var client1 = new PlayerSessionInfo(testSteamId, testName);
        CurrentSession.AddPlayer(client1);
        
        Debug.Log($"Is Session Full? {CurrentSession.IsSessionFull}");

        CurrentSession.AddPlayer(client1);

        CurrentSession.SetCustomProperty("Difficulty", "Extreme");
        CurrentSession.SetCustomProperty("MatchTimer", "300");
        Debug.Log($"Difficulty set to: {CurrentSession.GetCustomProperty("Difficulty")}");

        Debug.Log($"Everyone Ready? {CurrentSession.AllPlayersReady}");
        
        for (int i = 0; i < CurrentSession.Players.Count; i++)
        {
            var p = CurrentSession.Players[i];
            p.IsReady = true;
            CurrentSession.Players[i] = p;
        }
        Debug.Log($"Everyone Ready now? {CurrentSession.AllPlayersReady}");

        CurrentSession.ResetReadyStates();
        Debug.Log($"Post-Reset Ready Check: {CurrentSession.AllPlayersReady}");

        CurrentSession.RemovePlayer(99999);
        CurrentSession.RemovePlayer(testSteamId);

        string json = JsonConvert.SerializeObject(CurrentSession, Formatting.Indented);
        Debug.Log("<color=green>Session Data Serialized Successfully:</color>\n" + json);
    }

    [ContextMenu("Re-run Demo")]
    private void ManualDemo() => RunProductionDemo();
}