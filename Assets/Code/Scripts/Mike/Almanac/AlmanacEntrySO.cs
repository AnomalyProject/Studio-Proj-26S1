using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Almanac Entry", menuName = "Scriptable Objects/Almanac Entry")]
public class AlmanacEntrySO : CollectibleSO
{
    [SerializeField] private AlmanacType entryType;
    [SerializeField] private Sprite entryIcon;
    [SerializeField] private Sprite[] images;

    public AlmanacType EntryType => entryType;
    public IReadOnlyList<Sprite> Images => images;
    public Sprite EntryIcon => entryIcon;
}