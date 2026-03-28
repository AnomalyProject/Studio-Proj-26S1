using UnityEngine;
public class SaveInputTester : MonoBehaviour
{
    public PlayerSave playerSave;

    /*Testing Keys to see if works Save and Load old system
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            playerSave.Save("slotName1");
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            playerSave.Save("slotName2");
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            playerSave.Save(null);//new file by number test
        }


        if (Input.GetKeyDown(KeyCode.J))
        {
            playerSave.Load("slotName1");
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            playerSave.Load("slotName2");
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            playerSave.Load(null);//Most recent test
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SaveSystem.ResetAllSaves();//Reset for Testing
        }
    }*/

    //Testing Keys to see if works Save and Load new system
    public void OnSave1() => playerSave.Save("slotName1");
    public void OnSave2() => playerSave.Save("slotName2");
    public void OnSaveNew() => playerSave.Save(null); //new file by number test

    public void OnLoad1() => playerSave.Load("slotName1");
    public void OnLoad2() => playerSave.Load("slotName2");
    public void OnLoadRecent() => playerSave.Load(null);//Most recent test

    public void OnReset() => SaveSystem.ResetAllSaves();//Reset for Testing
}