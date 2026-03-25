using UnityEngine;

public class GameStateManagerController : MonoBehaviour
{
    private void Start()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("[StateManagerController]:You forgot the GameStateManager in the scene....Jenkins might be smarter than you, on god.");
            return;
        }

        GameStateManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void OnGUI()
    {
        if (GameStateManager.Instance == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 300));
        GUILayout.Label($"Current State: {GameStateManager.Instance.CurrentState}");

        if (GUILayout.Button("Menu")) GameStateManager.Instance.RequestStateChange(GameState.Menu);
        if (GUILayout.Button("Lobby")) GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        if (GUILayout.Button("Loading")) GameStateManager.Instance.RequestStateChange(GameState.Loading);
        if (GUILayout.Button("InGame")) GameStateManager.Instance.RequestStateChange(GameState.InGame);
        if (GUILayout.Button("PostGame")) GameStateManager.Instance.RequestStateChange(GameState.PostGame);

        GUILayout.EndArea();
    }

    // private void Update()
    // {
    //     if (GameStateManager.Instance == null) return;

    //     if (Input.GetKeyDown(KeyCode.Z)) GameStateManager.Instance.RequestStateChange(GameState.Lobby);
    //     if (Input.GetKeyDown(KeyCode.X)) GameStateManager.Instance.RequestStateChange(GameState.Loading);
    //     if (Input.GetKeyDown(KeyCode.C)) GameStateManager.Instance.RequestStateChange(GameState.InGame);
    //     if (Input.GetKeyDown(KeyCode.V)) GameStateManager.Instance.RequestStateChange(GameState.PostGame);
    //     if (Input.GetKeyDown(KeyCode.B)) GameStateManager.Instance.RequestStateChange(GameState.Menu);
    // }

    public void HandleStateChanged(GameState previous, GameState next)
    {
        Debug.Log($"[GameStateManagerController]: State Changed from {previous} to {next}");

    }
}