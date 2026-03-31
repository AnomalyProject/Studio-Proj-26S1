using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    // Public:
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public event Action<GameState, GameState> OnStateChanged; //(previous state, next state)

    // Private:
    private bool isTransitioning;

    private bool CanMakeTransition(GameState currentState, GameState nextState)
    {
        switch (currentState)
        {
            case GameState.Menu:
                return (nextState == GameState.Lobby);
            case GameState.Lobby:
                return (nextState == GameState.Loading || nextState == GameState.Menu);
            case GameState.Loading:
                return (nextState == GameState.InGame || nextState == GameState.Lobby);
            case GameState.InGame:
                return (nextState == GameState.PostGame);
            case GameState.PostGame:
                return (nextState == GameState.InGame || nextState == GameState.Lobby);
            default:
                return false;
        }
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