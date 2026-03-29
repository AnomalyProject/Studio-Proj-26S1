using PurrNet;
using UnityEngine;

public class PlayerSpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject[] spawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private LayerMask PlayerMask;
    private GameObject playerObj;
    private NetworkIdentity _spawnedObj;
    public NetworkManager netManager;
    protected override void OnSpawned()
    {
        if (!isServer) return;
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (netManager == null) netManager = FindFirstObjectByType<NetworkManager>();

            bool spawned = false;

            for (int i = 0; i < spawnPoint.Length; i++)
            {
                Vector3 pos = spawnPoint[i].transform.position;

                playerObj = Instantiate(playerPrefab, pos, Quaternion.identity);

                if(localPlayer != null) _spawnedObj.GiveOwnership(localPlayer);

                else { Debug.Log("Local player is null!");}

                spawned = true;
                break;
                
            }

            if (!spawned)
            {
                Debug.LogWarning("Some fatass is probably blocking the spawn point.");
            }

            if(!isOwner) Debug.Log("<color=red> ||| isOwner = false |||</color>");
            if(!isController) Debug.Log("not a controller");
    }
}