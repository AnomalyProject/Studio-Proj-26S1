using System;
using UnityEngine;
using UnityEngine.Events;

public class FSM_GameStates : MonoBehaviour
{
    private static FSM_GameStates instance;
    public gameStatesEnum currentState { get; private set; }
    public UnityAction<gameStatesEnum, gameStatesEnum> onStateChange; //(previous, next)

    private bool CanMakeTransition(gameStatesEnum currentState, gameStatesEnum nextState)
    {
        return currentState switch
        {
            gameStatesEnum.Menu => (nextState == gameStatesEnum.Lobby),
            gameStatesEnum.Lobby => (nextState == gameStatesEnum.Loading || nextState == gameStatesEnum.Menu),
            gameStatesEnum.Loading => (nextState == gameStatesEnum.InGame),
            gameStatesEnum.InGame => (nextState == gameStatesEnum.PostGame),
            gameStatesEnum.PostGame => (nextState == gameStatesEnum.Menu)
        };
    }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RequestStateChange(gameStatesEnum newState)
    {
        if (CanMakeTransition(currentState, newState))
        {
            Debug.Log("State change to " + newState);
            gameStatesEnum previousState = currentState;
            currentState = newState;
            onStateChange?.Invoke(previousState, newState);
        }
        else
        {
            Debug.Log("Cannot make transition from " + currentState + "to " + newState);
        }
    }
}
