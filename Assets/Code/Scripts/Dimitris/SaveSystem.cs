using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    //Define a fixed safe path that points to the folder where the game saves are
    public static readonly string SAVE_FOLDER = Application.persistentDataPath + "/Saves";
    public static readonly string SAVE_PREFIX = "SaveFile_";
    public static event Action<SaveData> OnSaveEvent, OnLoadEvent;
    static SaveSystem() => Init();

    public static void Init()
    {
        //Testing if folder of saves already exists and if not creating folder
        if (!Directory.Exists(SAVE_FOLDER))
        {
            Directory.CreateDirectory(SAVE_FOLDER);
        }
    }


    public static void Save(SaveData data, int slotIndex)
    {
        try //Trys to save in a certain Slot
        {
            string slotName = GetFileName(slotIndex);
            string path = Path.Combine(SAVE_FOLDER, slotName); //Creates a full file path
            //if (File.Exists(path)) File.Copy(path, path + ".bak", true); //Back ups in case already exists
            File.WriteAllText(path, JsonUtility.ToJson(data, true)); //Writes to a file and turns SaveData to Json String, Serialization
            Debug.Log("Saved: " + slotName);
            OnSaveEvent?.Invoke(data);
        }
        catch (Exception e)  //Error in case save doesn't work correclty
        {
            Debug.LogError("Failed to save: " + e.Message);
        }
    }
    public static SaveData Load(int slotIndex)  //Loads certain Slot
    {
        try
        {
            string path;
            string slotName = GetFileName(slotIndex);

            path = Path.Combine(SAVE_FOLDER, slotName);

            if (!File.Exists(path))
            {
                Debug.LogWarning("Save file not found: " + slotName);
                return null;
            }
            Debug.Log("Loaded:" + slotName);
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data != null)
            {
                OnLoadEvent?.Invoke(data);
                return data;
            }
            else return LoadLast();
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
            return null;
        }

    }

    public static SaveData LoadLast()
    {
        /*checks all the save files in the fileInfo , if a fileInfo has been more recent LastWriteTime from the last mostRecentFile
        , becomes the new mostRecentFile*/
        DirectoryInfo directoryInfo = new DirectoryInfo(SAVE_FOLDER);
        FileInfo[] saveFiles = directoryInfo.GetFiles("*.txt");
        FileInfo mostRecentFile = null;
        string path;
        string json;

        foreach (FileInfo fileInfo in saveFiles)
        {
            if (mostRecentFile == null)
            {
                mostRecentFile = fileInfo;
            }
            else if (fileInfo.LastWriteTime > mostRecentFile.LastWriteTime)
            {
                mostRecentFile = fileInfo;
            }
        }

        if (mostRecentFile != null)
        {
            path = mostRecentFile.FullName;//Sets path
            Debug.Log("Loaded:" + mostRecentFile.Name);
            json = File.ReadAllText(path); //Reads the file
            SaveData data = JsonUtility.FromJson<SaveData>(json); //Turns Json to SaveData ,Deserialization
            if (data != null) OnLoadEvent?.Invoke(data);
            return data;
        }
        else
        {
            Debug.Log("No save files found");
            return null;
        }
    }

    public static void DeleteSave(int slotIndex)
    {
        string filePath = Path.Combine(SAVE_FOLDER, GetFileName(slotIndex));

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Deleted save at slot {slotIndex}");
            return;
        }

        Debug.Log($"Save at slot {slotIndex} not found.");
    }
    public static void DeleteAllSaves()
    {
        if (Directory.Exists(SAVE_FOLDER))
        {
            DirectoryInfo direct = new DirectoryInfo(SAVE_FOLDER);
            foreach (FileInfo file in direct.GetFiles("*.txt")) //Deletes every save file from folder
            {
                file.Delete();
            }
            Debug.Log("All saves have been deleted!");
        }
    }
    static string GetFileName(int slotIndex) => $"{SAVE_PREFIX}{slotIndex}.txt";
    public static bool SaveExists(int slotIndex)
    {
        string path = Path.Combine(SAVE_FOLDER, GetFileName(slotIndex));
        return File.Exists(path);
    }
}
