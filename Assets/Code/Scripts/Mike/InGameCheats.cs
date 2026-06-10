using System;
using PurrNet;
using UnityEngine;

public class InGameCheats : MonoBehaviour
{
    private static bool Registered = false;
    [SerializeField] private bool cheatsEnabled = true;
    [SerializeField] private ItemData currencyItem;

    private void Awake()
    {
        if (Registered || !cheatsEnabled) return;

        DevConsole.CommandData richComm = new DevConsole.CommandData("Gives the player a lot of currency.", RichCheat);
        DevConsole.RegisterCommand("rich", richComm);

        DevConsole.CommandData winComm = new DevConsole.CommandData("Enables the win room. (server only)", WinGame);
        DevConsole.RegisterCommand("win", winComm);

        DevConsole.CommandData voidComm = new DevConsole.CommandData("Enables the void room. (server only)", VoidRoom);
        DevConsole.RegisterCommand("void", voidComm);

        DevConsole.CommandData mapVarComm = new DevConsole.CommandData("Change to a new map variation. (server only, optional args: true = with anomalies, false = no anomaly)", NextAnomaly);
        DevConsole.RegisterCommand("nextvar", mapVarComm);

        DevConsole.CommandData clearsave = new DevConsole.CommandData("Deletes all save files.", ClearSave);
        DevConsole.RegisterCommand("clearsave", clearsave);

        NoClip.CreateNoClip();

        Registered = true;
    }

    private void ClearSave(string[] obj) => RefrenceManager.DeleteAllSaves();
    private void RichCheat(string[] args)
    {
        if (PlayerBody.localPlayerBody) AddItemToPlayer_Server(PlayerBody.localPlayerBody, currencyItem, currencyItem.MaxStackSize);
    }
    private void NextAnomaly(string[] args)
    {
        if (!IsManagerValid(out AnomalyManager manager)) return;
        ResetPlayerPosition();
        manager.DecideNextMapVariation(withAnomalies: args.Length > 0 ? bool.Parse(args[0]) : true);
    }
    private void VoidRoom(string[] obj)
    {
        if (!IsManagerValid(out AnomalyManager manager)) return;
        ResetPlayerPosition();
        manager.EnablePunishmentRoom_Server();
    }

    private void WinGame(string[] obj)
    {
        if (!IsManagerValid(out AnomalyManager manager)) return;
        ResetPlayerPosition();
        manager.EnableWinRoom_Server();
    }

    #region Helpers

    [ServerRpc] private static void AddItemToPlayer_Server(PlayerBody player, ItemData item, int amount)
    {
        player.Inventory.Add(item, amount);
    }
    [ObserversRpc] private static void ResetPlayerPosition()
    {
        var localPlayer = PlayerBody.localPlayerBody;
        var controller = localPlayer.GetComponent<CharacterController>();
        controller.enabled = false;

        var entry = RefrenceManager.Instance.Gameplay.MapOrientor.EntryElevator;
        bool foundSpawn = false;

        for (int i = 0; i < entry.transform.childCount; i++)
        {
            if (entry.transform.GetChild(i).name.Contains("Spawn"))
            {
                Transform spawn = entry.transform.GetChild(i).transform;
                localPlayer.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                foundSpawn = true;
                break;
            }
        }

        if (!foundSpawn) localPlayer.transform.SetPositionAndRotation(entry.transform.position, entry.transform.rotation);
        controller.enabled = true;
    }
    private bool IsManagerValid(out AnomalyManager manager)
    {
        manager = RefrenceManager.Instance.Gameplay.AnomalyManager;

        if (!manager)
        {
            Debug.LogError("There is not an active instance of the anomaly manager");
            return false;
        }
        return true;
    }

    #endregion
}