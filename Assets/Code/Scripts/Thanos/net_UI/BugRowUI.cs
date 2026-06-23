using UnityEngine;
using TMPro;

public class BugRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void Setup(BugEntry bug)
    {
        titleText.text = bug.title;
        descriptionText.text = bug.description;
    }
}