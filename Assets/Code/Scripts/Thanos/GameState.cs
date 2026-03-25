public enum GameState
{
    Menu, // Main menu, no session active
    Lobby, // Players gathering, configuring match settings
    Loading, // Map/assets loading, all players confirmed ready
    InGame, // Active game session
    PostGame, // Round/match ended, showing results
}