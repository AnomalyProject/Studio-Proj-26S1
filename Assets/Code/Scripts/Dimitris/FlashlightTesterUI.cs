using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// Test Script that sets flashlight , slider and the text . Takes from flashlight the values and 
/// puts in the slider in order to show the UI
/// </summary>
public class FlashlightUI : MonoBehaviour
{
    [SerializeField] private Flashlight flashlight;
    [SerializeField] private Slider batterySlider;
    [SerializeField] private TextMeshProUGUI batteryText;

    private void Update()
    {
        float value = flashlight.NormalizedDurability;

        
        batterySlider.value = value;// Update Slider Value 


        batteryText.text = Mathf.RoundToInt(value * 100) + "%";//change 100% view from 0-1
    }
}