using UnityEngine;

public class GameStateManagerController : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text gameStateText;

    private void Start()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("[StateManagerController]:You forgot the GameStateManager in the scene....Jenkins might be smarter than you, on god.");
            return;
        }

        GameStateManager.Instance.OnStateChanged += HandleStateChanged;

        if (gameStateText != null)
        {
            gameStateText.text = $"State: {GameStateManager.Instance.CurrentState}";
        }
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void Update()
    {
        if (GameStateManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Z)) GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        if (Input.GetKeyDown(KeyCode.X)) GameStateManager.Instance.RequestStateChange(GameState.Loading);
        if (Input.GetKeyDown(KeyCode.C)) GameStateManager.Instance.RequestStateChange(GameState.InGame);
        if (Input.GetKeyDown(KeyCode.V)) GameStateManager.Instance.RequestStateChange(GameState.PostGame);
        if (Input.GetKeyDown(KeyCode.B)) GameStateManager.Instance.RequestStateChange(GameState.Menu);
    }

    public void HandleStateChanged(GameState previous, GameState next)
    {
        Debug.Log($"[GameStateManagerController]: State Changed from {previous} to {next}");
        if (gameStateText != null)
        {
            gameStateText.text = $"State: {next}";
        }

    }
}