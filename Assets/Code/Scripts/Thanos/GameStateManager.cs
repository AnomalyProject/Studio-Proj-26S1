using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    // Public:
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public event Action<GameState, GameState> OnStateChanged; //(previous, next)

    // Private:
    private bool isTransitioning;

    private bool CanMakeTransition(GameState currentState, GameState nextState)
    {
        return currentState switch
        {
            GameState.Menu => (nextState == GameState.Lobby),
            GameState.Lobby => (nextState == GameState.Loading || nextState == GameState.Menu),
            GameState.Loading => (nextState == GameState.InGame || nextState == GameState.Lobby),
            GameState.InGame => (nextState == GameState.PostGame),
            GameState.PostGame => (nextState == GameState.Menu || nextState == GameState.Lobby),
            _ => false
        };
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CurrentState = GameState.Menu;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RequestStateChange(GameState newState)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[GameStateManager]: State change rejected. Transition already in progress.");
            return;
        }

        if (CanMakeTransition(CurrentState, newState))
        {
            isTransitioning = true;

            try
            {
                Debug.Log("[GameStateManager]:State change to " + newState);
                GameState previousState = CurrentState;
                CurrentState = newState;
                OnStateChanged?.Invoke(previousState, newState);
            }
            finally
            {
                isTransitioning = false;
            }

        }
        else
        {
            Debug.LogWarning("[GameStateManager]: Cannot make transition from " + CurrentState + " to " + newState);
        }
    }
}