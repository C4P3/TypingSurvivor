using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.Game.Settings;

namespace TypingSurvivor.Features.Game.Gameplay
{
    // GameManagerはゲーム状態の「書き込み」のみに責任を持つ
    public class GameManager : NetworkBehaviour, IGameStateWriter
    {
        private GameState _gameState;

        #region SerializeField
        [SerializeField] private GameConfig _gameConfig;
        #endregion

        private float oxygenDecreaseRate = 0.9f;

        private IGameModeStrategy _gameModeStrategy;

        /// <summary>
        /// 手動DIのための初期化メソッド。
        /// </summary>
        public void Initialize(GameState gameState, IGameModeStrategy gameModeStrategy)
        {
            _gameState = gameState;
            _gameModeStrategy = gameModeStrategy;
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                // クライアント側の処理（もしあれば）
                _gameState.CurrentPhase.OnValueChanged += HandlePhaseChanged_Client;
            }
            if (IsServer)
            {
                StartCoroutine(ServerGameLoop());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient && _gameState != null)
            {
                _gameState.CurrentPhase.OnValueChanged -= HandlePhaseChanged_Client;
            }
        }

        // --- Writerインターフェースの実装 ---
        public void AddOxygen(float amount)
        {
            if (!IsServer) return;
            _gameState.OxygenLevel.Value = Mathf.Clamp(_gameState.OxygenLevel.Value + amount, 0, 100);
        }

        public void AddScore(ulong clientId, int amount)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.Score += amount;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }
        public void SetPlayerGameOver(ulong clientId)
        {
            // TODO: 実装
        }

        private IEnumerator ServerGameLoop()
        {
            _gameState.CurrentPhase.Value = GamePhase.WaitingForPlayers;
            while (NetworkManager.Singleton.ConnectedClients.Count < _gameModeStrategy.PlayerCount)
            {
                yield return null;
            }

            _gameState.CurrentPhase.Value = GamePhase.Countdown;
            yield return new WaitForSeconds(5);

            _gameState.CurrentPhase.Value = GamePhase.Playing;
            while (true)
            {
                _gameState.OxygenLevel.Value -= oxygenDecreaseRate * Time.deltaTime;
                
                // IGameStateReaderをGameStateが持つようになったので、
                // IsGameOverの引数をGameStateに変更する必要があるかもしれない
                if (_gameModeStrategy.IsGameOver(_gameState))
                {
                    break;
                }
                
                yield return null;
            }

            _gameState.CurrentPhase.Value = GamePhase.Finished;
        }
        
        private void HandlePhaseChanged_Client(GamePhase previousPhase, GamePhase newPhase)
        {
            Debug.Log($"Game phase changed to: {newPhase}");
        }
    }
}