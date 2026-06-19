using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlmanacEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI entryName, entryDescription;
    [SerializeField] private Image entryIcon, newIcon;
    [SerializeField] private Image[] images;

    public void Setup(AlmanacRegistry.AlmanacEntryInfo info)
    {
        for (int i = 0; i < images.Length; i++)
        {
            bool canApply = info.discovered && i < info.entryData.Images.Count;

            if (!canApply)
            {
                images[i].gameObject.SetActive(false);
                continue;
            }

            images[i].sprite = info.entryData.Images[i];
        }

        newIcon.gameObject.SetActive(info.discovered && !info.viewed);

        if (!info.discovered) return;

        entryName.text = info.entryData.CollectibleName;
        entryDescription.text = info.entryData.Description;
        entryIcon.sprite = info.entryData.EntryIcon;
    }
}