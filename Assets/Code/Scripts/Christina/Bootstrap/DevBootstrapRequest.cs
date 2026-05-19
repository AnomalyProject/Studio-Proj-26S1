public enum DevLaunchMode {Solo, DevHost, DevClient}

[System.Serializable]
public class DevBootstrapRequest
{
    
    public const string LaunchRequestPrefKey =  "Anomaly.DevBootstrap.LaunchRequest";
    public const string LegacyDevScenePrefKey = "Christina.DevScenePath";
    public const string NextJoinIndexPrefKey = "Anomaly.DevBootstrap.NextJoinIndex";
    
    public DevLaunchMode mode;
    public string scenePath;        
    public string runtimeSceneName; 
    public string address = "127.0.0.1";
    public int port = 5000;
    public int maxPlayers = 2;
    // 0 for host, 1++ for clients
    public int playerIndex; 
    public ulong fakeSteamId;
    public string displayName;
}