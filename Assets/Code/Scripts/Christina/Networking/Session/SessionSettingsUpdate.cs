/// <summary>
///Identifies which settings field a single update targets. 
/// </summary>
public enum SessionSettingsField
{
    MapName,
    GameMode,
    MaxPlayers,
    LobbyVisibility,
    Custom
}

/// <summary>
///Typed representation of one settings change after parsing.
/// SessionManager translates the RPC into this shape. The service then reads
/// only the file that matches the Field enum value. 
/// </summary>
public class SessionSettingsUpdate
{
    public SessionSettingsField Field;
    public string MapName;
    public string GameMode;
    public int MaxPlayers;
    public LobbyVisibility Visibility;
    public string CustomKey;
    public string CustomValue;
}
