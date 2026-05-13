using UnityEngine;

public class InGameCheats : MonoBehaviour
{
    private static bool Registered = false;

    private void Awake()
    {
        if (Registered) return;

        DevConsole.CommandData mapVarComm = new DevConsole.CommandData("Change to a new map variation with anomalies.", NextAnomaly);
        DevConsole.RegisterCommand("nextvar", mapVarComm);

        Registered = true;
    }

    private void NextAnomaly(string[] args)
    {
        if (!IsManagerValid(out AnomalyManager manager)) return;
        manager.DecideNextMapVariation(withAnomalies: true);
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
}