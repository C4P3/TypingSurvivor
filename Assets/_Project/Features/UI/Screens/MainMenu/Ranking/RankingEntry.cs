using TMPro;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens
{
    public class RankingEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        public void Initialize(int rank, string playerName, int score)
        {
            _rankText.text = rank.ToString();
            _playerNameText.text = playerName;
            _scoreText.text = score.ToString();
        }
    }
}
