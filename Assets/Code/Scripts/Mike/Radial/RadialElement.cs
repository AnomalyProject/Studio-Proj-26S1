using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadialElement : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI labelText;
    [SerializeField] Image icon;

    public Image bg;

    public void SetData(Sprite icon, string label)
    {
        this.icon.sprite = icon;
        labelText.text = label;
    }

    public void SetBGColor(Color color) => bg.color = color;
}