
using System;
using TMPro;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Represents a single item in the profile selection list.
    /// </summary>
    public class ProfileListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _profileNameText;
        [SerializeField] private InteractiveButton _button;

        private Action<string> _onSelected;
        private string _profileId;

        public void Initialize(string displayName, Action<string> onSelected, string profileId)
        {
            _profileId = profileId;
            _profileNameText.text = displayName;
            _onSelected = onSelected;
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            _onSelected?.Invoke(_profileId);
        }
    }
}
