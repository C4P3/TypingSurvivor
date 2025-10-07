using UnityEngine;
using Unity.Netcode;
using TypingSurvivor.Features.Core.PlayerStatus;

using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.InGameHUD
{
    [RequireComponent(typeof(Canvas))]
    public class InGameHUDManager : ScreenBase
    {
        [SerializeField] private OxygenView _oxygenView;
        [SerializeField] private PlayerInfoView _playerInfoView;


        private IGameStateReader _gameStateReader;
        private IPlayerStatusSystemReader _playerStatusReader;
        
        private Canvas _canvas;
        
        public ulong PlayerOwnerId { get; private set; }

        #region Initialization and Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _canvas = GetComponent<Canvas>();
            _canvas.worldCamera = Camera.main; // Default camera, will be overridden
        }

        public void SetPlayerOwnerId(ulong ownerId)
        {
            PlayerOwnerId = ownerId;
        }

        public void SetRenderCamera(UnityEngine.Camera camera)
        {
            if (_canvas != null)
            {
                _canvas.worldCamera = camera;
            }
        }

        public void Initialize(IGameStateReader gameStateReader, IPlayerStatusSystemReader playerStatusReader)
        {
            _gameStateReader = gameStateReader;
            _playerStatusReader = playerStatusReader;
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnSubscribeEvents();
        }

        #endregion

        #region Public API for Display

        public void UpdatePlayerName(string playerName)
        {
            if (_playerInfoView != null) _playerInfoView.SetName(playerName);
        }

        public void UpdatePlayerRating(int rating, bool isRankedMatch)
        {
            if (_playerInfoView == null) return;

            if (isRankedMatch)
            {
                _playerInfoView.SetRating(rating);
            }
            else
            {
                _playerInfoView.HideRating();
            }
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeEvents()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.OnOxygenChanged += OnOxygenChanged;
                _gameStateReader.PlayerDatas.OnListChanged += OnPlayerDatasChanged;
            }
        }

        private void UnSubscribeEvents()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.OnOxygenChanged -= OnOxygenChanged;
                _gameStateReader.PlayerDatas.OnListChanged -= OnPlayerDatasChanged;
            }
        }

        #endregion

        #region Event Handlers
        private void OnPlayerDatasChanged(NetworkListEvent<TypingSurvivor.Features.Game.Gameplay.Data.PlayerData> changeEvent)
        {
            // Find the specific data for this HUD's owner and update the name
            foreach (var playerData in _gameStateReader.PlayerDatas)
            {
                if (playerData.ClientId == PlayerOwnerId)
                {
                    UpdatePlayerName(playerData.PlayerName.ToString());
                    return; // Found our player, no need to loop further
                }
            }
        }

        private void OnOxygenChanged(ulong clientId, float newOxygenValue)
        {
            if (clientId != PlayerOwnerId) return;
            if (_playerStatusReader == null) return;

            _oxygenView.UpdateView(newOxygenValue, _playerStatusReader.GetStatValue(PlayerOwnerId, PlayerStat.MaxOxygen));
        }

        #endregion
    }
}