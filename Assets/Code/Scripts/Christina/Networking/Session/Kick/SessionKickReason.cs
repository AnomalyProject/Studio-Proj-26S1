public enum SessionKickReason
{
    AfkNotParticipating,
    PreventingGroupFromContinuing,
    HarassmentAbusiveCommunication,
    CheatingOrExploiting,
    Other
}

public static class SessionKickReasonExtensions
{
    public static string ToDisplayText(this SessionKickReason reason)
    {
        switch (reason)
        {
            case SessionKickReason.AfkNotParticipating:
                return "AFK / not participating";
            case SessionKickReason.PreventingGroupFromContinuing:
                return "Preventing the group from continuing";
            case SessionKickReason.HarassmentAbusiveCommunication:
                return "Harassment / abusive communication";
            case SessionKickReason.CheatingOrExploiting:
                return "Cheating / exploiting";
            default:
                return "Other";
        }
    }
}
