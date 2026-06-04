using UnityEngine;

public class NarrationEvent : MonoBehaviour
{
    [SerializeField] NarrationEntry entry;

    public void PlayNarration()
    {
        if(NarratorManager.Instance && entry)
        NarratorManager.Instance.TriggerNarration(entry);
    }
}