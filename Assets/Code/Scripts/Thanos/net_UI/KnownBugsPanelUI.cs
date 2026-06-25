using UnityEngine;
using TMPro;

public class KnownBugsPanelUI : MonoBehaviour
{
    [SerializeField] private KnownBugsData bugsData;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private BugRowUI rowPrefab;

    [SerializeField] private GameObject messageState;
    [SerializeField] private TMP_Text messageText;

    private void OnEnable() => Populate();

    private void Populate()
    {
        ClearRows();

        if (bugsData == null || bugsData.bugs == null || bugsData.bugs.Count == 0)
        {
            ShowMessage("No known bugs.");
            return;
        }

        messageState.SetActive(false);
        foreach (var bug in bugsData.bugs)
            if (!string.IsNullOrWhiteSpace(bug.title))
            {
                var row = Instantiate(rowPrefab, rowsParent);
                row.Setup(bug);
            }
    }

    private void ShowMessage(string msg)
    {
        messageState.SetActive(true);
        messageText.text = msg;
    }

    private void ClearRows()
    {
        for (int i = rowsParent.childCount - 1; i >= 0; i--)
            Destroy(rowsParent.GetChild(i).gameObject);
    }
}