using TMPro;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens.InGameHUD
{
    public class PlayerInfoView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _playerRatingText;

        public void SetName(string playerName)
        {
            if (_playerNameText != null)
            {
                _playerNameText.text = playerName;
            }
        }

        public void SetRating(int rating)
        {
            if (_playerRatingText != null)
            {
                _playerRatingText.text = $"Rating: {rating}";
            }
        }

        public void HideRating()
        {
            if (_playerRatingText != null)
            {
                _playerRatingText.gameObject.SetActive(false);
            }
        }
    }
}
