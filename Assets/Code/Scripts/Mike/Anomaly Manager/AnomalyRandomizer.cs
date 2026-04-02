using UnityEngine;

public class AnomalyRandomizer : MonoBehaviour
{
    [SerializeField] GameObject[] anomalies;

    private void OnEnable()
    {
        foreach (GameObject a in anomalies) a.SetActive(Random.Range(0, 2) == 0);
    }
}
