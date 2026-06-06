using UnityEngine;

public class CollectibleDisplay : MonoBehaviour
{
    [SerializeField] private CollectibleSO collectibleData;

    private void Awake() => UpdateVisuals();

    public void UpdateVisuals()
    {
        if (collectibleData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool hasCollectible = RefrenceManager.CurrentSave.collectiblesGathered.Contains(collectibleData.ID);
        gameObject.SetActive(hasCollectible);
    }
}