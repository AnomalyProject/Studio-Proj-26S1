using UnityEngine;

public class PlayerBodyCallbackTester : MonoBehaviour
{
    private PlayerBody localPlayerBody;

    private void OnEnable()
    {
        PlayerBody.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        PlayerBody.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
    }

    private void OnDisable()
    {
        PlayerBody.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        PlayerBody.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
    }

    private void HandleLocalPlayerSpawned(PlayerBody playerBody)
    {
        localPlayerBody = playerBody;
        Debug.Log($"[LocalPlayerBodyCallbackTester] Local player spawned: {playerBody.name}");
    }

    private void HandleLocalPlayerDespawned(PlayerBody playerBody)
    {
        if (localPlayerBody == playerBody)
        {
            localPlayerBody = null;
        }

        Debug.Log($"[LocalPlayerBodyCallbackTester] Local player despawned: {playerBody.name}");
    }
}
