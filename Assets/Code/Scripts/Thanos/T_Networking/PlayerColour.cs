using UnityEngine;

public static class PlayerColour
{
    public static readonly Color[] Colors = new Color[]
    {
        new Color(1.0f, 0.5f, 0.0f),
        new Color(0.5f, 0.0f, 0.5f),
        Color.yellow,
        Color.blue
    };
    
    public static readonly string[] HexColors = new string[]
    {
        "#FF8000",
        "#800080",
        "#FFFF00",
        "#0000FF"
    };

    public static Color GetColor(int index) => Colors[Mathf.Clamp(index, 0, 3)];
    public static string GetHex(int index) => HexColors[Mathf.Clamp(index, 0, 3)];
}