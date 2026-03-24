using UnityEngine;

public class GameStateManagerController : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) GameStateManager.instance.RequestStateChange(GameState.Lobby);
        if (Input.GetKeyDown(KeyCode.X)) GameStateManager.instance.RequestStateChange(GameState.Loading);
        if (Input.GetKeyDown(KeyCode.C)) GameStateManager.instance.RequestStateChange(GameState.InGame);
        if (Input.GetKeyDown(KeyCode.V)) GameStateManager.instance.RequestStateChange(GameState.PostGame);
        if (Input.GetKeyDown(KeyCode.B)) GameStateManager.instance.RequestStateChange(GameState.Menu);
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        Debug.Log($"[GameStateManagerController]: State Changed from {previous } to {next}");
    }
}