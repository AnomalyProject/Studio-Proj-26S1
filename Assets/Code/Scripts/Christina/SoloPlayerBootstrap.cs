using UnityEngine;

public class SoloPlayerBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject soloPlayerPrefab;
    [SerializeField] private Transform soloSpawnPoint;

    private void Start()
    {
        if (SessionModeManager.Instance == null) return;
        if (SessionModeManager.Instance.CurrentMode != SessionMode.Solo) return;

        if (soloPlayerPrefab == null || soloSpawnPoint == null)
        {
            Debug.LogError("[SoloPlayerBootstrap] Missing prefab or spawn point.");
            return;
        }

        Instantiate(soloPlayerPrefab, soloSpawnPoint.position, soloSpawnPoint.rotation);
    }
}