using System;

[AttributeUsage(AttributeTargets.Field)]
class InputContextConfigAttribute : Attribute
{
    public string MapName { get; }
    public bool CursorVisible { get; }

    public InputContextConfigAttribute(string mapName, bool cursorVisible)
    {
        MapName = mapName;
        CursorVisible = cursorVisible;
    }
}