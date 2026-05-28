

public class SessionSettingsService
{
    #region Dependencies

    private readonly SessionStateStore sessionStore;

    #endregion

    #region Constructor
    public SessionSettingsService(SessionStateStore sessionStore)
    {
        this.sessionStore = sessionStore;
    }
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Applies the update if valid. The result reports success or failure and
    /// on success whether the caller should reset ready states after   
    /// </summary>
     public SessionSettingsResult TryApply(SessionSettingsUpdate update)
      {
          if (!sessionStore.HasSession)
              return SessionSettingsResult.Failed(SessionErrorCode.InvalidState, "No active session.");

          SessionData session = sessionStore.Current;

          switch (update.Field)
          {
              case SessionSettingsField.MapName:
                  session.MapName = update.MapName;
                  return SessionSettingsResult.Succeeded(shouldResetReady: true);

              case SessionSettingsField.GameMode:
                  session.GameMode = update.GameMode;
                  return SessionSettingsResult.Succeeded(shouldResetReady: true);

              case SessionSettingsField.MaxPlayers:
                  if (update.MaxPlayers < 2 || update.MaxPlayers > 4)
                      return SessionSettingsResult.Failed(SessionErrorCode.Unknown, "Max players must be between 2 and 4.");

                  if (update.MaxPlayers < session.Players.Count)
                      return SessionSettingsResult.Failed(SessionErrorCode.Unknown, "Max players cannot be lower than the current player count.");

                  session.MaxPlayers = update.MaxPlayers;
                  return SessionSettingsResult.Succeeded(shouldResetReady: true);

              case SessionSettingsField.LobbyVisibility:
                  string visibilityValue = update.Visibility == LobbyVisibility.Public ? "Public" : "Friends Only";
                  session.SetCustomProperty("LobbyVisibility", visibilityValue);
                  return SessionSettingsResult.Succeeded(shouldResetReady: false);

              case SessionSettingsField.Custom:
                  session.SetCustomProperty(update.CustomKey, update.CustomValue);
                  return SessionSettingsResult.Succeeded(shouldResetReady: false);

              default:
                  return SessionSettingsResult.Failed(SessionErrorCode.Unknown, "Unknown settings field.");
          }
      }
    
    #endregion
}

/// <summary>
/// Result of SessionSettingsService.TryApply. Same Success/ErrorCode/Message shape
/// as SessionCommandResult with one extra field that tells the caller if they need
/// to reset ready states.
/// </summary>
public struct SessionSettingsResult
{
    #region Fields

    public bool Success;
    public bool ShouldResetReadyStates;
    public SessionErrorCode ErrorCode;
    public string Message;

    #endregion

    #region Factories

    public static SessionSettingsResult Succeeded(bool shouldResetReady)
    {
        SessionSettingsResult result = new SessionSettingsResult();
        result.Success = true;
        result.ShouldResetReadyStates = shouldResetReady;
        return result;
    }

    public static SessionSettingsResult Failed(SessionErrorCode code, string message)
    {
        SessionSettingsResult result = new SessionSettingsResult();
        result.Success = false;
        result.ErrorCode = code;
        result.Message = message;
        return result;
    }

    #endregion
}