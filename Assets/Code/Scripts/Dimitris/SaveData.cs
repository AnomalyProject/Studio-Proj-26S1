///<summary>
///This is Script that is been used like a data container. For now has only position for testing, later more will be added whatever needs to be saved 
///</summary>
using System.Collections.Generic;
using UnityEngine;

[System .Serializable]
public class SaveData
{
    public Vector3 Position;
    public Dictionary<string, string> ExtraData = new Dictionary<string, string>(); //This Dictionary is for later for modular Objects
}
