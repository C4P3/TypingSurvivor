using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.Game.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Tooltip("管理対象のカメラ。CameraFollowコンポーネントが付いていること。")]
        [SerializeField] private List<CameraFollow> _cameras;

        public event Action<ulong, UnityEngine.Camera> OnCameraAssigned;

        private IGameStateReader _gameStateReader;

        public void Initialize(IGameStateReader gameStateReader)
        {
            _gameStateReader = gameStateReader;
        }

        public UnityEngine.Camera GetCameraForPlayer(ulong clientId)
        {
            foreach (var camFollow in _cameras)
            {
                if (camFollow.gameObject.activeInHierarchy && camFollow.TargetFacade != null && camFollow.TargetFacade.OwnerClientId == clientId)
                {
                    return camFollow.GetComponent<UnityEngine.Camera>();
                }
            }
            return null;
        }

        public Dictionary<ulong, UnityEngine.Camera> GetAssignedCameras()
        {
            var assignedCameras = new Dictionary<ulong, UnityEngine.Camera>();
            foreach (var camFollow in _cameras)
            {
                if (camFollow.gameObject.activeInHierarchy && camFollow.TargetFacade != null)
                {
                    assignedCameras[camFollow.TargetFacade.OwnerClientId] = camFollow.GetComponent<UnityEngine.Camera>();
                }
            }
            return assignedCameras;
        }

        private void Start()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.SpawnedPlayers.OnListChanged += HandlePlayersChanged;
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
            foreach (var cam in _cameras)
            {
                cam.gameObject.SetActive(false);
                cam.TargetFacade = null;
            }

            int playerCount = players.Count;
            if (playerCount == 0) return;

            var sortedPlayers = players.OrderBy(p => p.OwnerClientId).ToList();

            switch (playerCount)
            {
                case 1:
                    SetupSinglePlayerCamera(sortedPlayers[0]);
                    break;
                case 2:
                    SetupTwoPlayerSplitScreen(sortedPlayers[0], sortedPlayers[1]);
                    break;
                case 3:
                case 4:
                    SetupFourPlayerSplitScreen(sortedPlayers);
                    break;
                default:
                    SetupFourPlayerSplitScreen(sortedPlayers);
                    break;
            }
        }

        private void SetupSinglePlayerCamera(PlayerFacade player)
        {
            if (_cameras.Count < 1) return;
            var cam1 = _cameras[0];
            var cameraComponent = cam1.GetComponent<UnityEngine.Camera>();
            cam1.gameObject.SetActive(true);
            cameraComponent.rect = new Rect(0, 0, 1, 1);
            cam1.Target = player.transform;
            cam1.TargetFacade = player;
            
            var listener = cam1.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = player.IsOwner;

            OnCameraAssigned?.Invoke(player.OwnerClientId, cameraComponent);
        }

        private void SetupTwoPlayerSplitScreen(PlayerFacade player1, PlayerFacade player2)
        {
            if (_cameras.Count < 2) return;

            var cam1 = _cameras[0];
            var cameraComponent1 = cam1.GetComponent<UnityEngine.Camera>();
            cam1.gameObject.SetActive(true);
            cameraComponent1.rect = new Rect(0, 0, 0.5f, 1);
            cam1.Target = player1.transform;
            cam1.TargetFacade = player1;
            var listener1 = cam1.GetComponent<AudioListener>();
            if (listener1 != null) listener1.enabled = player1.IsOwner;
            OnCameraAssigned?.Invoke(player1.OwnerClientId, cameraComponent1);

            var cam2 = _cameras[1];
            var cameraComponent2 = cam2.GetComponent<UnityEngine.Camera>();
            cam2.gameObject.SetActive(true);
            cameraComponent2.rect = new Rect(0.5f, 0, 0.5f, 1);
            cam2.Target = player2.transform;
            cam2.TargetFacade = player2;
            var listener2 = cam2.GetComponent<AudioListener>();
            if (listener2 != null) listener2.enabled = player2.IsOwner;
            OnCameraAssigned?.Invoke(player2.OwnerClientId, cameraComponent2);
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
                var player = players[i];
                var cam = _cameras[i];
                var cameraComponent = cam.GetComponent<UnityEngine.Camera>();
                cam.gameObject.SetActive(true);
                cameraComponent.rect = rects[i];
                cam.Target = player.transform;
                cam.TargetFacade = player;
                
                var listener = cam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = player.IsOwner;
                OnCameraAssigned?.Invoke(player.OwnerClientId, cameraComponent);
            }
        }
    }
}
