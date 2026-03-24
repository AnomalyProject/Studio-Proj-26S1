using UnityEngine;

public class GameStateManagerController : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text _gameStateText;

    private void Start()
    {
        if (GameStateManager.instance == null)
        {
            Debug.LogError("You forgot the GameStateManager in the scene....Jenkins might be smarter than you, on god.");
            return;
        }

        GameStateManager.instance.onStateChanged += HandleStateChanged;
        
        if (_gameStateText != null)
        {
            _gameStateText.text = $"State: {GameStateManager.instance.CurrentState}";
        }
    }

    private void OnDestroy()
    {
        if (GameStateManager.instance != null)
        {
            GameStateManager.instance.onStateChanged -= HandleStateChanged;
        }
    }

    private void Update()
    {
        if (GameStateManager.instance == null) return;

        if (Input.GetKeyDown(KeyCode.Z)) GameStateManager.instance.RequestStateChange(GameState.Lobby);
        if (Input.GetKeyDown(KeyCode.X)) GameStateManager.instance.RequestStateChange(GameState.Loading);
        if (Input.GetKeyDown(KeyCode.C)) GameStateManager.instance.RequestStateChange(GameState.InGame);
        if (Input.GetKeyDown(KeyCode.V)) GameStateManager.instance.RequestStateChange(GameState.PostGame);
        if (Input.GetKeyDown(KeyCode.B)) GameStateManager.instance.RequestStateChange(GameState.Menu);
    }

    public void HandleStateChanged(GameState previous, GameState next)
    {
        Debug.Log($"[GameStateManagerController]: State Changed from {previous} to {next}");
        if (_gameStateText != null)
        {
            _gameStateText.text = $"State: {next}";
        }
        
    }
}