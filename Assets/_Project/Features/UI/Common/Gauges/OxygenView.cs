using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OxygenView : MonoBehaviour
{
    [SerializeField] private Slider _oxygenSlider;
    [SerializeField] private TextMeshProUGUI _oxygenLabel;

    // InGameHUDManagerから呼び出される唯一の公開メソッド
    public void UpdateView(float currentOxygen, float maxOxygen)
    {
        _oxygenSlider.maxValue = maxOxygen;
        _oxygenSlider.value = currentOxygen;
        _oxygenLabel.text = $"{Mathf.CeilToInt(currentOxygen)} / {maxOxygen}";
    }
}