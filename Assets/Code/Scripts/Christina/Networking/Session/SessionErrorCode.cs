using System;

public enum SessionErrorCode
{
    None, // default state, it means no error
    SessionFull,
    InvalidState,
    NotHost,
    PlayerNotFound,
    PlayersNotReady,
    AlreadyInSession,
    Unknown
}

[System.Serializable]
public struct SessionErrorResponse
{
    public SessionErrorCode Code;
    public string Message;

    public SessionErrorResponse(SessionErrorCode code, string message)
    {
        Code = code;
        Message = message;
    }
}