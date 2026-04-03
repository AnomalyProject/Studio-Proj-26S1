/// <summary>
/// Describes where am I in startup
/// enum = labels
/// </summary>
public enum HostStartupStage
{
    Idle,
    HostStartRequest,
    NetworkManagerFound,
    TransportValidated,
    HostStarting,
    HostReady,
    LobbyCreateRequested,
    LobbyCreated,
    LobbyEnteredAsHost,
    HostPublished,
    Failed
}

/// <summary>
/// Describes what stage am I in, what attempt is this, what failed and what transport am I using?
/// </summary>
public struct HostStartupStatus
{
    public int AttemptID;
    public HostStartupStage Stage;
    public HostStartupStage FailureStage;
    public string Message;
    public string ActiveTransport;
}

public enum JoinStartupStage
{
    Idle,
    JoinRequestReceived,
    LeavingPreviousLobby,
    LobbyJoinRequested,
    LobbyEntered,
    TransportConnectStarting,
    TransportConnected,
    SessionJoinRequested,
    SessionJoinApproved,
    Failed
}


public struct JoinStartupStatus
{
    public int AttemptID;
    public JoinStartupStage Stage;
    public ConnectionFailureSource FailureSource;
    public string Message;
    public string TargetLobbyId;
}

public enum ConnectionFailureSource
{
    None,
    Steam,
    Transport,
    SessionApproval,
    Unknown
}



