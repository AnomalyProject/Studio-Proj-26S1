using System.Collections;
using UnityEngine;
using PurrNet;

public class ElevatorLobbyManager : NetworkBehaviour
{
    [SerializeField] private ElevatorExit elevatorExit;
    [SerializeField] private float loadDelay = 2f;
    
    private Coroutine loadCoroutine;
    
    /*private void OnEnable()
    {
        SessionManager.OnServerSessionChanged += CheckElevatorReady;
    }

    private void OnDisable()
    {
        SessionManager.OnServerSessionChanged -= CheckElevatorReady;
    }*/
    
    public void PlayerEntered(PlayerID playerID)
    {
        SessionManager.Instance.SetPlayerInElevator(playerID, true);
    }

    public void PlayerExited(PlayerID playerID)
    {
        SessionManager.Instance.SetPlayerInElevator(playerID, false);

        if (SessionManager.Instance.CurrentElevatorState == ElevatorLobbyState.DoorsClosing) CancelDeparture();
    }
    
    /*private void CheckElevatorReady()
    {
        if (!isServer) return;
        if (SessionManager.Instance == null) return;

        if (SessionManager.Instance.CanStartElevatorSequence())
        {
            StartDeparture();
        }
    }*/
    
    private void StartDeparture()
    {
        if (SessionManager.Instance.CurrentElevatorState != ElevatorLobbyState.Open) return;

        SessionManager.Instance.SetElevatorState(ElevatorLobbyState.DoorsClosing);
        PlayCloseDoors();
    }
    
    private void CancelDeparture()
    {
        if (loadCoroutine != null)
        {
            StopCoroutine(loadCoroutine);
            loadCoroutine = null;
        }

        SessionManager.Instance.SetElevatorState(ElevatorLobbyState.Open);
        PlayOpenDoors();
    }
    
    public void OnDoorsFullyClosed()
    {
        if (!isServer) return;
        if (SessionManager.Instance.CurrentElevatorState != ElevatorLobbyState.DoorsClosing) return;

        SessionManager.Instance.SetElevatorState(ElevatorLobbyState.DoorsClosed);
        loadCoroutine = StartCoroutine(LoadAfterDelay());
    }
    
    private IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(loadDelay);
        SessionManager.Instance.TryStartMatchFromServer();
    }

    private void PlayCloseDoors()
    {
        if (elevatorExit == null) return;

        elevatorExit.CloseDoors();
        CloseDoors_ObserversRpc();
    }

    private void PlayOpenDoors()
    {
        if (elevatorExit == null) return;

        elevatorExit.OpenDoors();
        OpenDoors_ObserversRpc();
    }
    
    public void RequestDeparture()
    {
        if (!isServer) return;
        if (SessionManager.Instance == null) return;

        if (!SessionManager.Instance.CanStartElevatorSequence())
            return;

        StartDeparture();
    }

    [ObserversRpc(runLocally: false)]
    private void CloseDoors_ObserversRpc()
    {
        if (elevatorExit != null) elevatorExit.CloseDoors();
    }

    [ObserversRpc(runLocally: false)]
    private void OpenDoors_ObserversRpc()
    {
        if (elevatorExit != null) elevatorExit.OpenDoors();
    }
}
