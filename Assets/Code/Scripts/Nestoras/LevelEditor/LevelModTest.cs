using UnityEngine;

public class LevelModTest : MonoBehaviour
{
    [SerializeField] private GameObject variationToToggle;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) variationToToggle.SetActive(!variationToToggle.activeInHierarchy);
    }
}
