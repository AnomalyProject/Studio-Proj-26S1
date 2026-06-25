public struct SessionCommandResult
{
    #region Fields

    public bool Success;
    public SessionErrorCode ErrorCode;
    public string Message;

    #endregion
    
    #region Factories
    
    public static SessionCommandResult Succeeded()
    {
        SessionCommandResult result = new SessionCommandResult();
        result.Success = true;
        result.ErrorCode = SessionErrorCode.None;
        result.Message = "";
        return result;
    }

    public static SessionCommandResult Failed(SessionErrorCode code, string message)
    {
        SessionCommandResult result = new SessionCommandResult();
        result.Success = false;
        result.ErrorCode = code;
        result.Message = message;
        return result;
    }
    
    #endregion
}
