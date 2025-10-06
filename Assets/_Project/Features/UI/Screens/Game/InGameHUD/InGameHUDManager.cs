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
        [SerializeField] private ScoreView _scoreView;
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
                _gameStateReader.OnScoreChanged += OnScoreChanged;
            }
        }

        private void UnSubscribeEvents()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.OnOxygenChanged -= OnOxygenChanged;
                _gameStateReader.OnScoreChanged -= OnScoreChanged;
            }
        }

        #endregion

        #region Event Handlers
        private void OnOxygenChanged(ulong clientId, float newOxygenValue)
        {
            if (clientId != PlayerOwnerId) return;
            if (_playerStatusReader == null) return;

            _oxygenView.UpdateView(newOxygenValue, _playerStatusReader.GetStatValue(PlayerOwnerId, PlayerStat.MaxOxygen));
        }

        private void OnScoreChanged(int newScoreValue)
        {
            _scoreView.UpdateView(newScoreValue);
        }

        #endregion
    }
}