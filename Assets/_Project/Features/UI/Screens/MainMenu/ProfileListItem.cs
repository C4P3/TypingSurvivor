
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
        private string _profileName;

        public void Initialize(string profileName, Action<string> onSelected)
        {
            _profileName = profileName;
            _profileNameText.text = profileName;
            _onSelected = onSelected;
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            _onSelected?.Invoke(_profileName);
        }
    }
}
