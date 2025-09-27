using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.Game.Player;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Camera
{
    /// <summary>
    /// ゲームのカメラレイアウト（画面分割など）を管理し、各カメラが正しいプレイヤーを追従するように設定する。
    /// GameStateを監視し、適切なタイミングで自律的に動作する。
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Tooltip("管理対象のカメラ。CameraFollowコンポーネントが付いていること。")]
        [SerializeField] private List<CameraFollow> _cameras;

        private IGameStateReader _gameStateReader;

        public void Initialize(IGameStateReader gameStateReader)
        {
            _gameStateReader = gameStateReader;
        }

        private void Start()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.SpawnedPlayers.OnListChanged += HandlePlayersChanged;
                // Initial setup with current players
                HandlePlayersChanged(new NetworkListEvent<NetworkObjectReference>());
            }
        }

        private void OnDestroy()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.SpawnedPlayers.OnListChanged -= HandlePlayersChanged;
            }
        }

        private void HandlePlayersChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
        {
            var playerFacades = new List<PlayerFacade>();
            foreach (var playerRef in _gameStateReader.SpawnedPlayers)
            {
                if (playerRef.TryGet(out var networkObject))
                {
                    if (networkObject.TryGetComponent<PlayerFacade>(out var playerFacade))
                    {
                        playerFacades.Add(playerFacade);
                    }
                }
            }
            SetupCameras(playerFacades);
        }

        private void SetupCameras(IReadOnlyList<PlayerFacade> players)
        {
            // まず全てのカメラを非アクティブ化
            foreach (var cam in _cameras)
            {
                cam.gameObject.SetActive(false);
            }

            int playerCount = players.Count;
            if (playerCount == 0) return;

            // プレイヤーIDの昇順でソートし、ホストが常にP1になるようにする
            var sortedPlayers = players.OrderBy(p => p.OwnerClientId).ToList();

            switch (playerCount)
            {
                case 1:
                    SetupSinglePlayerCamera(sortedPlayers[0].transform);
                    break;
                case 2:
                    SetupTwoPlayerSplitScreen(sortedPlayers[0].transform, sortedPlayers[1].transform);
                    break;
                // 3人や4人の場合も同様に実装
                case 3:
                case 4:
                    SetupFourPlayerSplitScreen(sortedPlayers);
                    break;
                default:
                    // 5人以上は最初の4人のみ表示
                    SetupFourPlayerSplitScreen(sortedPlayers);
                    break;
            }
        }

        private void SetupSinglePlayerCamera(Transform player)
        {
            if (_cameras.Count < 1) return;
            var cam1 = _cameras[0];
            cam1.gameObject.SetActive(true);
            cam1.GetComponent<UnityEngine.Camera>().rect = new Rect(0, 0, 1, 1);
            cam1.Target = player;
        }

        private void SetupTwoPlayerSplitScreen(Transform player1, Transform player2)
        {
            if (_cameras.Count < 2) return;

            var cam1 = _cameras[0];
            cam1.gameObject.SetActive(true);
            cam1.GetComponent<UnityEngine.Camera>().rect = new Rect(0, 0, 0.5f, 1);
            cam1.Target = player1;

            var cam2 = _cameras[1];
            cam2.gameObject.SetActive(true);
            cam2.GetComponent<UnityEngine.Camera>().rect = new Rect(0.5f, 0, 0.5f, 1);
            cam2.Target = player2;
        }

        private void SetupFourPlayerSplitScreen(IReadOnlyList<PlayerFacade> players)
        {
            if (_cameras.Count < 4) return;

            var rects = new Rect[]
            {
                new Rect(0, 0.5f, 0.5f, 0.5f),    // 左上
                new Rect(0.5f, 0.5f, 0.5f, 0.5f), // 右上
                new Rect(0, 0, 0.5f, 0.5f),       // 左下
                new Rect(0.5f, 0, 0.5f, 0.5f)     // 右下
            };

            for (int i = 0; i < Mathf.Min(players.Count, 4); i++)
            {
                var cam = _cameras[i];
                cam.gameObject.SetActive(true);
                cam.GetComponent<UnityEngine.Camera>().rect = rects[i];
                cam.Target = players[i].transform;
            }
        }
    }
}
