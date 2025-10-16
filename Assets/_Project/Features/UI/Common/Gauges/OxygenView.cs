using UnityEngine;
using UnityEngine.UI;

public class OxygenView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _oxygenSlider;
    [SerializeField] private Image _fillImage; // The fill image of the slider

    [Header("Color Settings")]
    [SerializeField] private Color _fullOxygenColor = Color.green;
    [SerializeField] private Color _lowOxygenColor = Color.yellow;
    [SerializeField] private Color _criticalOxygenColor = Color.red;

    [Header("Animation Settings")]
    [SerializeField] private float _updateSpeed = 8f; // Speed of the smooth update

    private float _targetValue = 1f;

    private void Update()
    {
        // Smoothly interpolate the slider's value towards the target value
        if (Mathf.Abs(_oxygenSlider.value - _targetValue) > 0.001f)
        {
            _oxygenSlider.value = Mathf.Lerp(_oxygenSlider.value, _targetValue, Time.deltaTime * _updateSpeed);
        }
        else
        {
            _oxygenSlider.value = _targetValue;
        }
    }

    public void UpdateView(float currentOxygen, float maxOxygen)
    {
        if (maxOxygen <= 0) return;

        _oxygenSlider.maxValue = 1f; // Normalize to 0-1 range for easier interpolation
        _targetValue = currentOxygen / maxOxygen;

        // Change color based on oxygen percentage
        if (_fillImage != null)
        {
            float oxygenPercentage = _targetValue;
            if (oxygenPercentage <= 0.2f) // 20% or less
            {
                _fillImage.color = _criticalOxygenColor;
            }
            else if (oxygenPercentage <= 0.5f) // 50% or less
            {
                _fillImage.color = _lowOxygenColor;
            }
            else // Above 50%
            {
                _fillImage.color = _fullOxygenColor;
            }
        }
    }
}