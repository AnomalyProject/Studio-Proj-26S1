using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    //Define a fixed safe path that points to the folder where the game saves are
    public static readonly string SAVE_FOLDER = Application.persistentDataPath + "/Saves";

    public static void Init()
    {
        //Testing if folder of saves already exists and if not creating folder
        if(!Directory.Exists(SAVE_FOLDER))
        {
            Directory.CreateDirectory(SAVE_FOLDER);
        }
    }
    public static void Save(SaveData data, string slotName)
    {
        try //Trys to save in a certain Slot
        {
            
            if (string.IsNullOrEmpty(slotName))//if none slotname is been given then checks first avalaible new file by number 
            {
                int saveNumber = 1;
                while (File.Exists(SAVE_FOLDER + "/slotName" + saveNumber + ".txt")) 
                {
                    saveNumber++;
                }
                slotName = "slotName" + saveNumber;
            }
            string path = Path.Combine(SAVE_FOLDER, slotName + ".txt"); //Creates a full file path
            if (File.Exists(path))
                File.Copy(path, path + ".bak", true); //Back ups in case already exists
            File.WriteAllText(path, JsonUtility.ToJson(data, true)); //Writes to a file and turns SaveData to Json String, Serialization
            Debug.Log("Saved: " + slotName);
        }
        catch (Exception e)  //Error in case save doesn't work correclty
        {
            Debug.LogError("Failed to save: " + e.Message);
        }
    }
    public static SaveData Load(string slotName = null)  //Loads certain Slot or most recent
    {
        try
        {
            string path;
            if (!string.IsNullOrEmpty(slotName))
            {
                path = Path.Combine(SAVE_FOLDER, slotName + ".txt"); //Loads certain Slotname if not null
                if (!File.Exists(path))
                {
                    Debug.LogWarning("Save file not found: " + slotName);
                    return null;
                }
                Debug.Log("Loaded:" + slotName);
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(json);
                
            }
            else
            {
                /*checks all the save files in the fileInfo , if a fileInfo has been more recent LastWriteTime from the last mostRecentFile
                , becomes the new mostRecentFile*/
                DirectoryInfo directoryInfo = new DirectoryInfo(SAVE_FOLDER);
                FileInfo[] saveFiles = directoryInfo.GetFiles();
                FileInfo mostRecentFile = null;
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
                path = mostRecentFile.FullName;//Sets path
                if (mostRecentFile != null)
                {
                    Debug.Log("Loaded:" + slotName);
                    string json = File.ReadAllText(path); //Reads the file
                    return JsonUtility.FromJson<SaveData>(json);  //Turns Json to SaveData ,Deserialization
                }
                else
                {
                    Debug.Log("No save files found");
                    return null;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
            return null;
        }

    }
    public static void ResetAllSaves()
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
}
