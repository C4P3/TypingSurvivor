using TMPro;
using UnityEngine;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens.Result
{
    public class PlayerResultCard : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI _playerNameText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _wpmText;
        [SerializeField] private TextMeshProUGUI _blocksDestroyedText;
        [SerializeField] private TextMeshProUGUI _missRateText;

        [Header("Rating (Optional)")]
        [SerializeField] private GameObject _ratingSection;
        [SerializeField] private TextMeshProUGUI _ratingChangeText;

        public void Populate(Game.Gameplay.Data.PlayerData playerData, bool isRanked, int newRating)
        {
            if (_playerNameText != null)
            {
                _playerNameText.text = playerData.PlayerName.ToString();
            }

            if (_wpmText != null)
            {
                float wpm = 0;
                if (playerData.TotalTimeTyping > 0) wpm = (playerData.TotalCharsTyped / 5.0f) / (playerData.TotalTimeTyping / 60.0f);
                _wpmText.text = $"{wpm:F1}";
            }

            if (_blocksDestroyedText != null)
            {
                _blocksDestroyedText.text = $"{playerData.BlocksDestroyed}";
            }
            
            if (_missRateText != null)
            {
                float missRate = 0;
                if (playerData.TotalKeyPresses > 0) missRate = (float)playerData.TypingMisses / playerData.TotalKeyPresses * 100.0f;
                _missRateText.text = $"{playerData.TypingMisses} ({missRate:F1}%)";
            }

            if (_ratingSection != null)
            {
                _ratingSection.SetActive(isRanked);
                if (isRanked && _ratingChangeText != null)
                {
                    // TODO: Pass old rating to calculate and show the change.
                    _ratingChangeText.text = $"NewRate → {newRating}";
                }
            }
        }
    }
}
