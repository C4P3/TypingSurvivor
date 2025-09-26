using UnityEngine;
using TMPro;

public class ScoreView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreLabel;

    // InGameHUDManagerから呼び出される唯一の公開メソッド
    public void UpdateView(int currentScore)
    {
        _scoreLabel.text = $"{currentScore}";
    }
}