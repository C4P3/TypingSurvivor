using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.Game.Settings;
using TypingSurvivor.Features.Game.Level; // ILevelServiceのために追加

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class GameManager : NetworkBehaviour, IGameStateWriter
    {
        private GameState _gameState;
        private IGameModeStrategy _gameModeStrategy;
        private ILevelService _levelService; // LevelManagerへの参照
        private Grid _grid;

        [SerializeField] private GameConfig _gameConfig;
        private float oxygenDecreaseRate = 0.9f;

        public void Initialize(GameState gameState, IGameModeStrategy gameModeStrategy, ILevelService levelService)
        {
            _gameState = gameState;
            _gameModeStrategy = gameModeStrategy;
            _levelService = levelService;
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                _gameState.CurrentPhase.OnValueChanged += HandlePhaseChanged_Client;
            }
            if (IsServer)
            {
                _grid = FindObjectOfType<Grid>();
                NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCheck;
                StartCoroutine(ServerGameLoop());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient && _gameState != null)
            {
                _gameState.CurrentPhase.OnValueChanged -= HandlePhaseChanged_Client;
            }
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback -= ConnectionApprovalCheck;
            }
        }

        private void ConnectionApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (_gameState.CurrentPhase.Value != GamePhase.WaitingForPlayers)
            {
                response.Approved = false;
                response.Reason = "Game has already started.";
                return;
            }
            if (NetworkManager.Singleton.ConnectedClients.Count >= _gameModeStrategy.PlayerCount)
            {
                response.Approved = false;
                response.Reason = "The session is full.";
                return;
            }
            response.Approved = true;
            response.CreatePlayerObject = false; // 自動スポーンは無効
        }

        private IEnumerator ServerGameLoop()
        {
            _gameState.CurrentPhase.Value = GamePhase.WaitingForPlayers;
            while (NetworkManager.Singleton.ConnectedClients.Count < _gameModeStrategy.PlayerCount)
            {
                yield return null;
            }

            // 全プレイヤーの接続が完了したら、スポーン処理を開始
            yield return StartCoroutine(SpawnPlayers());

            _gameState.CurrentPhase.Value = GamePhase.Countdown;
            yield return new WaitForSeconds(5);

            _gameState.CurrentPhase.Value = GamePhase.Playing;
            while (true)
            {
                _gameState.OxygenLevel.Value -= oxygenDecreaseRate * Time.deltaTime;
                if (_gameModeStrategy.IsGameOver(_gameState))
                {
                    break;
                }
                yield return null;
            }

            _gameState.CurrentPhase.Value = GamePhase.Finished;
        }

        private IEnumerator SpawnPlayers()
        {
            var clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            int playerCount = clientIds.Count;

            // ゲームモードに応じてスポーン戦略を選択
            ScriptableObject spawnStrategy = _gameModeStrategy.PlayerCount > 1
                ? _gameConfig.VersusSpawnStrategy
                : _gameConfig.SinglePlayerSpawnStrategy;

            var spawnPoints = _levelService.GetSpawnPoints(playerCount, spawnStrategy);

            if (spawnPoints.Count < playerCount)
            {
                Debug.LogError("スポーン地点の数がプレイヤー数に足りません！");
                // TODO: エラーハンドリング
                yield break;
            }

            for (int i = 0; i < playerCount; i++)
            {
                ulong clientId = clientIds[i];
                Vector3Int gridPos = spawnPoints[i];

                // スポーン地点の周辺を更地にする
                _levelService.ClearArea(gridPos, 1); // 3x3マス

                // グリッド中央のワールド座標を取得
                Vector3 spawnPos = _grid.GetCellCenterWorld(gridPos);

                // プレイヤーを生成してスポーン
                GameObject playerInstance = Instantiate(_gameConfig.PlayerPrefab, spawnPos, Quaternion.identity);
                playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            }
            yield return null;
        }

        // --- IGameStateWriterの実装 ---
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
        
        private void HandlePhaseChanged_Client(GamePhase previousPhase, GamePhase newPhase)
        {
            Debug.Log($"Game phase changed to: {newPhase}");
        }
    }
}