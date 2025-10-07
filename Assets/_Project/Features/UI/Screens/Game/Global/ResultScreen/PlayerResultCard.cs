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
            _playerNameText.text = playerData.PlayerName.ToString();

            float wpm = 0;
            if (playerData.TotalTimeTyping > 0) wpm = (playerData.TotalCharsTyped / 5.0f) / (playerData.TotalTimeTyping / 60.0f);
            _wpmText.text = $"WPM: {wpm:F1}";

            _blocksDestroyedText.text = $"Blocks: {playerData.BlocksDestroyed}";
            
            float missRate = 0;
            if (playerData.TotalKeyPresses > 0) missRate = (float)playerData.TypingMisses / playerData.TotalKeyPresses * 100.0f;
            _missRateText.text = $"Miss: {playerData.TypingMisses} ({missRate:F1}%)";

            if (isRanked)
            {
                _ratingSection.SetActive(true);
                // TODO: Pass old rating to calculate and show the change.
                _ratingChangeText.text = $"RATING → {newRating}";
            }
            else
            {
                _ratingSection.SetActive(false);
            }
        }
    }
}
