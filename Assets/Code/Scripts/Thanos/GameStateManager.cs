using System;
using UnityEngine;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    // Public:
    public static GameStateManager instance {get; private set;}
    public GameState CurrentState {get; private set;}
    public event Action<GameState, GameState> onStateChanged; //(previous, next)
    
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
            GameState.PostGame => (nextState == GameState.Menu || nextState == GameState.Lobby)
            
        };
    }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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
        if (CanMakeTransition(CurrentState, newState))
        {
            isTransitioning = true;

            try
            {
                Debug.Log("State change to " + newState);
                GameState previousState = CurrentState;
                CurrentState = newState;
                onStateChanged?.Invoke(previousState, newState);
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