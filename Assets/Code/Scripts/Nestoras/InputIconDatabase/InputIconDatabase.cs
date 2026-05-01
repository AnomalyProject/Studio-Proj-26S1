using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Database of icons for different input devices, mapped to input actions
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/Input Icon Database")]
public class InputIconDatabase : ScriptableObject
{
    [Serializable]
    public struct InputIconMapping
    {
        public string controlPath;
        public Sprite icon;
    }

    [Header("Per Control Scheme")]
    public List<InputIconMapping> keyboard = new List<InputIconMapping>();
    public List<InputIconMapping> xbox = new List<InputIconMapping>();
    public List<InputIconMapping> playstation = new List<InputIconMapping>();
    public List<InputIconMapping> switchPro = new List<InputIconMapping>();

    private Dictionary<string, Sprite> lookup;

    public void BuildLookup(string scheme)
    {
        lookup = new Dictionary<string, Sprite>();
        List<InputIconMapping> activeList = scheme switch
        {
            "Keyboard&Mouse" => keyboard,
            "Gamepad" => xbox,
            "DualShock" => playstation,
            "Switch" => switchPro,
            _ => keyboard
        };
        foreach (InputIconMapping map in activeList) lookup[map.controlPath] = map.icon;
    }

    public Sprite GetIcon(string controlPath)
    {
        if (lookup == null) return null;
        lookup.TryGetValue(controlPath, out Sprite sprite);
        return sprite;
    }
}