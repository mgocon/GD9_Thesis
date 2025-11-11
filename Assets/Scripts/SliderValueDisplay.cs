using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Slider))]
public class SliderValueDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI valueLabel;
    
    [Header("Display Settings")]
    [SerializeField] private bool showAsPercentage = true;
    [SerializeField] private int decimalPlaces = 0;
    
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (slider != null)
            slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void Start()
    {
        UpdateValueDisplay(slider != null ? slider.value : 0f);
    }

    private void OnSliderValueChanged(float value)
    {
        UpdateValueDisplay(value);
    }

    private void UpdateValueDisplay(float normalizedValue)
    {
        if (valueLabel == null) return;

        float displayValue = showAsPercentage ? normalizedValue * 100f : normalizedValue;
        string formattedValue = displayValue.ToString($"F{decimalPlaces}");
        valueLabel.text = showAsPercentage ? $"{formattedValue}%" : formattedValue;
    }

    public void ForceUpdateDisplay()
    {
        if (slider != null)
            UpdateValueDisplay(slider.value);
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }
}
