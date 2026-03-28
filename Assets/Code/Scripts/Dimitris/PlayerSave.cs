///<summary>
///Connects Player with SaveSystem 
///</summary>
using UnityEngine;
using System;

public class PlayerSave : MonoBehaviour
{
    public Transform player;

    void Start()
    {
        SaveSystem.Init();
    }

    public static event Action<SaveData> OnSaveEvent;
    public static event Action<SaveData> OnLoadEvent;
    public void Save(string slotName = null)
    {
        //Creates new data and saves that , with new position
        SaveData data = new SaveData();
        data.Position = player.position;
       
        SaveSystem.Save(data,slotName);
        OnSaveEvent?.Invoke(data); //event triggers for save
    }
    
    public void Load(string slotName = null)
    {   //Brings data from file and transform to saved position
        CharacterController controller = GetComponent<CharacterController>();
        SaveData data = SaveSystem.Load(slotName);
        if (data != null )
        {
            controller.enabled = false;//disables controller for not to override position
            player.position = data.Position;
            controller.enabled = true;//renables controller

            OnLoadEvent?.Invoke(data); //event triggers for load
        }
    }
  
    }
