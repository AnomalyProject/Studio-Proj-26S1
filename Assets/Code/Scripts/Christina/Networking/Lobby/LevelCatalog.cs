using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Networking/Level Catalog", fileName = "LevelCatalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField] private List<LevelDefinition> levels = new();

    public IReadOnlyList<LevelDefinition> Levels => levels;
    public int LevelCount => levels.Count;
    
    public bool TryGetLevel(int index, out LevelDefinition level)
    {
        level = null;

        if (index < 0 || index >= levels.Count) return false;

        level = levels[index];
        return level != null;
    }

    public bool TryGetById(string id, out LevelDefinition level)
    {
        level = null;

        if (string.IsNullOrWhiteSpace(id)) return false;

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null && levels[i].Id == id)
            {
                level = levels[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetDefault(out LevelDefinition level)
    {
        level = null;

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null)
            {
                level = levels[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetNextLevel(string currentId, int direction, out LevelDefinition level)
    {
        level = null;
        if (levels.Count == 0) return false;

        int step = direction < 0 ? -1 : 1;
        int startIndex = FindIndex(currentId);

        if (startIndex < 0) return TryGetDefault(out level);

        for (int offset = 1; offset <= levels.Count; offset++)
        {
            int index = (startIndex + step * offset + levels.Count) % levels.Count;
            LevelDefinition candidate = levels[index];

            if (candidate != null)
            {
                level = candidate;
                return true;
            }
        }

        return false;
    }

    private int FindIndex(string id)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null && levels[i].Id == id) return i;
        }

        return -1;
    }
}

[Serializable]
public class LevelDefinition
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private int mapIndex;
    [SerializeField] private Sprite preview;

    public string Id => id;
    public string DisplayName => displayName;
    public int MapIndex => mapIndex;
    public Sprite Preview => preview;
}