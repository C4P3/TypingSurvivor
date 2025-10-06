using UnityEngine;
using UnityEngine.UI;
// using TMPro;

public class OxygenView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _oxygenSlider;
    // [SerializeField] private TextMeshProUGUI _oxygenLabel;
    [SerializeField] private Image _fillImage; // The fill image of the slider

    [Header("Color Settings")]
    [SerializeField] private Color _fullOxygenColor = Color.green;
    [SerializeField] private Color _lowOxygenColor = Color.yellow;
    [SerializeField] private Color _criticalOxygenColor = Color.red;

    public void UpdateView(float currentOxygen, float maxOxygen)
    {
        if (maxOxygen <= 0) return;

        _oxygenSlider.maxValue = maxOxygen;
        _oxygenSlider.value = currentOxygen;
        // _oxygenLabel.text = $"{Mathf.CeilToInt(currentOxygen)} / {maxOxygen}";

        float oxygenPercentage = currentOxygen / maxOxygen;

        // Change color based on oxygen percentage
        if (_fillImage != null)
        {
            if (oxygenPercentage <= 0.2f) // 10% or less
            {
                _fillImage.color = _criticalOxygenColor;
            }
            else if (oxygenPercentage <= 0.5f) // 30% or less
            {
                _fillImage.color = _lowOxygenColor;
            }
            else // Above 30%
            {
                _fillImage.color = _fullOxygenColor;
            }
        }
    }
}