using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Collectible", menuName = "Scriptable Objects/Collectible")]
public class CollectibleSO : ScriptableObject
{
    [SerializeField, HideInInspector] private string _id;
    [SerializeField] private string _collectibleName;
    [SerializeField, TextArea(2,5)] private string _description;

    public string ID => _id;
    public string CollectibleName => _collectibleName;
    public string Description => _description;

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        #if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        if (!string.IsNullOrEmpty(path)) _id = AssetDatabase.AssetPathToGUID(path);
        #endif
    }
}