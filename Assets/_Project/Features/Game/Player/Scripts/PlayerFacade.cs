using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using TypingSurvivor.Features.Game.Player.Input;

namespace TypingSurvivor.Features.Game.Player
{
    // FacadeはNetworkBehaviourを継承し、ネットワーク通信の起点となる
    public class PlayerFacade : NetworkBehaviour
    {
        #region Events
        public static event Action<ulong, Vector3> OnPlayerMoved_Server;
        public static event Action<ulong, Vector3> OnPlayerSpawned_Server;
        public static event Action<ulong> OnPlayerDespawned_Server;
        #endregion

        // --- 参照 ---
        [Header("コンポーネント参照")]
        [SerializeField] private PlayerInput _input;
        [SerializeField] private PlayerStateMachine _stateMachine;
        [SerializeField] private PlayerView _view;

        // --- サーバーサイドの依存関係 ---
        private ILevelService _levelService;
        private Grid _grid;

        // --- 同期変数 ---
        private readonly NetworkVariable<PlayerState> _currentState = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<Vector3Int> NetworkGridPosition = new(writePerm: NetworkVariableWritePermission.Server);

        // --- 内部状態 ---
        [Header("Movement Settings")]
        [SerializeField] private float _moveDuration = 0.25f; // 1タイル移動するのにかかる時間
        private bool _isMoving_Server; // サーバー側での移動状態フラグ

        #region Properties for States
        // ステートマシン内の各Stateが必要とする情報へのアクセスを提供
        public Grid Grid => _grid;
        public float MoveDuration => _moveDuration;
        #endregion

        #region Unity & Network Callbacks

        private void Awake()
        {
            // ステートマシンの初期化
            InitializeStateMachine();
        }

        public override void OnNetworkSpawn()
        {
            _currentState.OnValueChanged += OnStateChanged;

            if (IsServer)
            {
                _levelService = FindObjectsOfType<MonoBehaviour>().OfType<ILevelService>().FirstOrDefault();
                _grid = FindObjectOfType<Grid>();
                if (_levelService == null) Debug.LogError("ILevelServiceの実装が見つかりません。");
                if (_grid == null) Debug.LogError("Gridが見つかりません。");

                // 初期位置とステートを設定
                NetworkGridPosition.Value = _grid.WorldToCell(transform.position);
                _currentState.Value = PlayerState.Roaming;
            }

            if (IsOwner)
            {
                _input.enabled = true;
                _input.OnMoveIntent += HandleMoveIntent;
                _input.OnInteractIntent += HandleInteractIntent;
                if(IsServer)
                {
                    OnPlayerSpawned_Server?.Invoke(OwnerClientId, transform.position);
                }
                else
                {
                    RequestSpawnedServerRpc();
                }
            }
            
            // 初期ステートを反映
            OnStateChanged(PlayerState.Roaming, _currentState.Value);
        }

        public override void OnNetworkDespawn()
        {
            _currentState.OnValueChanged -= OnStateChanged;

            if (IsOwner)
            {
                _input.enabled = false;
                _input.OnMoveIntent -= HandleMoveIntent;
                _input.OnInteractIntent -= HandleInteractIntent;
            }
            
            if(IsServer)
            {
                OnPlayerDespawned_Server?.Invoke(OwnerClientId);
            }
        }

        private void Update()
        {
            // 現在のステートのExecute処理を毎フレーム実行
            _stateMachine?.Execute();
        }

        #endregion

        private void InitializeStateMachine()
        {
            var states = new IPlayerState[]
            {
                new RoamingState(),
                new MovingState(this, transform),
                new TypingState()
            };
            _stateMachine = new PlayerStateMachine(states);
        }

        #region Event Handlers & RPCs

        private void HandleMoveIntent(Vector2 moveDirection)
        {
            if (moveDirection == Vector2.zero) return;

            Vector3Int directionInt;
            if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
            {
                directionInt = new Vector3Int((int)Mathf.Sign(moveDirection.x), 0, 0);
            }
            else
            {
                directionInt = new Vector3Int(0, (int)Mathf.Sign(moveDirection.y), 0);
            }
            
            RequestMoveServerRpc(directionInt);
        }

        private void HandleInteractIntent()
        {
            RequestInteractServerRpc();
        }
        
        [ServerRpc]
        private void RequestSpawnedServerRpc()
        {
            OnPlayerSpawned_Server?.Invoke(OwnerClientId, this.gameObject.transform.position);
        }

        [ServerRpc]
        private void RequestMoveServerRpc(Vector3Int moveDirection)
        {
            if (_isMoving_Server) return;
            if (_levelService == null || _grid == null) return;

            Vector3Int currentGridPos = NetworkGridPosition.Value;
            Vector3Int targetGridPos = currentGridPos + moveDirection;

            if (_levelService.IsWalkable(targetGridPos))
            {
                _isMoving_Server = true;
                NetworkGridPosition.Value = targetGridPos;
                _currentState.Value = PlayerState.Moving;

                StartCoroutine(FinishMovement_Server());
            }
        }

        private IEnumerator FinishMovement_Server()
        {
            yield return new WaitForSeconds(_moveDuration);
            
            // 移動後の位置を確定し、イベントを発行
            transform.position = _grid.GetCellCenterWorld(NetworkGridPosition.Value);
            OnPlayerMoved_Server?.Invoke(OwnerClientId, transform.position);

            _currentState.Value = PlayerState.Roaming;
            _isMoving_Server = false;
        }

        [ServerRpc]
        private void RequestInteractServerRpc()
        {
            Debug.Log("サーバーがクライアントからのインタラクト要求を受信しました。");
            // _currentState.Value = PlayerState.Typing;
        }

        private void OnStateChanged(PlayerState previousValue, PlayerState newValue)
        {
            Debug.Log($"State changed from {previousValue} to {newValue} on client {NetworkManager.LocalClientId}");
            _stateMachine.ChangeState(newValue);
            // _view.UpdateAnimation(newValue); // Viewへの通知はStateクラスの責務にしても良い
        }

        #endregion
    }
}
