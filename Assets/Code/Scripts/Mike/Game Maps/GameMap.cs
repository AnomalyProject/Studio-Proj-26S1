using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System;

public class GameMap : NetworkBehaviour
{
    [SerializeField] Transform entryPointAnchor, exitPointAnchor;
    [SerializeField] AudioClip mapMusicTheme;
    [SerializeField, Tooltip("Optional Alamanc Entry")] private AlmanacEntrySO _almanacEntry;
    [SerializeField] private Transform[] currencySpawnPoints;
    public AlmanacEntrySO AlmanacEntry => _almanacEntry;
    public AudioClip MapMusicTheme => mapMusicTheme;
    public Transform EntryPointAnchor => entryPointAnchor;
    public Transform ExitPointAnchor => exitPointAnchor;
    bool HasAnchorPoints => exitPointAnchor != null && entryPointAnchor != null;
    public IReadOnlyCollection<Transform> CurrencySpawnPoints => currencySpawnPoints;
    public static event Action<GameMap> OnMapLoaded;

    protected virtual void Awake()
    {
        entryPointAnchor.gameObject.SetActive(false);
        exitPointAnchor.gameObject.SetActive(false);
    }
    private void OnEnable() => OnMapLoaded?.Invoke(this);

    private void OnValidate()
    {
        if (!HasAnchorPoints)
        {
            Debug.LogWarning("You must assign entry & exit point anchors to the anomaly map!");
            return;
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying || !HasAnchorPoints) return;
    
        if (entryPointAnchor.position != Vector3.zero)
        {
            Debug.LogWarning("Entry point must exist in the center of the world!");
            entryPointAnchor.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        entryPointAnchor.transform.localScale = Vector3.one;
        exitPointAnchor.transform.localScale = Vector3.one;
    }
}
