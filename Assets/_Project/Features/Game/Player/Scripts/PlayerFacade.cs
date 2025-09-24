using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using TypingSurvivor.Features.Game.Player.Input;

namespace TypingSurvivor.Features.Game.Player
{
    public class PlayerFacade : NetworkBehaviour
    {
        #region Events
        public static event Action<ulong, Vector3> OnPlayerMoved_Server;
        public static event Action<ulong, Vector3> OnPlayerSpawned_Server;
        public static event Action<ulong> OnPlayerDespawned_Server;
        #endregion

        [Header("コンポーネント参照")]
        [SerializeField] private PlayerInput _input;
        [SerializeField] private PlayerStateMachine _stateMachine;
        [SerializeField] private PlayerView _view;

        // --- サーバーサイドの依存関係 ---
        private ILevelService _levelService;
        private IPlayerStatusSystemReader _statusReader;
        private Grid _grid;

        // --- 同期変数 ---
        private readonly NetworkVariable<PlayerState> _currentState = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<Vector3Int> NetworkGridPosition = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> NetworkMoveDuration = new(0.25f, writePerm: NetworkVariableWritePermission.Server);

        // --- クライアントサイドの内部状態 ---
        private bool _isMoveInputHeld;

        // --- サーバーサイドの内部状態 ---
        private Vector3Int _continuousMoveDirection_Server;
        private bool _isMoving_Server;

        #region Properties for States
        public Grid Grid => _grid;
        public float MoveDuration => NetworkMoveDuration.Value;
        #endregion

        #region Unity & Network Callbacks

        private void Awake()
        {
            InitializeStateMachine();
        }

        public override void OnNetworkSpawn()
        {
            _currentState.OnValueChanged += OnStateChanged;
            NetworkGridPosition.OnValueChanged += OnGridPositionChanged;

            // Gridへの参照はサーバー・クライアントの両方で必要
            _grid = FindObjectOfType<Grid>();
            if (_grid == null) Debug.LogError("Gridが見つかりません。");

            if (IsServer)
            {
                _levelService = FindObjectsOfType<MonoBehaviour>().OfType<ILevelService>().FirstOrDefault();
                _statusReader = FindObjectsOfType<MonoBehaviour>().OfType<IPlayerStatusSystemReader>().FirstOrDefault();
                if (_levelService == null) Debug.LogError("ILevelServiceの実装が見つかりません。");
                if (_statusReader == null) Debug.LogError("IPlayerStatusSystemReaderの実装が見つかりません。");

                NetworkGridPosition.Value = _grid.WorldToCell(transform.position);
                _currentState.Value = PlayerState.Roaming;
            }

            if (IsOwner)
            {
                _input.enabled = true;
                _input.OnMovePerformed += HandleMovePerformed;
                _input.OnMoveCanceled += HandleMoveCanceled;
                _input.OnInteractIntent += HandleInteractIntent;
                
                if(IsServer) OnPlayerSpawned_Server?.Invoke(OwnerClientId, transform.position);
                else RequestSpawnedServerRpc();
            }
            
            OnStateChanged(PlayerState.Roaming, _currentState.Value);
        }

        public override void OnNetworkDespawn()
        {
            _currentState.OnValueChanged -= OnStateChanged;
            NetworkGridPosition.OnValueChanged -= OnGridPositionChanged;

            if (IsOwner)
            {
                _input.enabled = false;
                _input.OnMovePerformed -= HandleMovePerformed;
                _input.OnMoveCanceled -= HandleMoveCanceled;
                _input.OnInteractIntent -= HandleInteractIntent;
            }
            
            if(IsServer) OnPlayerDespawned_Server?.Invoke(OwnerClientId);
        }

        private void Update()
        {
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

        private void HandleMovePerformed(Vector2 moveDirection)
        {
            // Vector2を最も近い8方向に丸める
            Vector2 normalized = moveDirection.normalized;
            int x = Mathf.RoundToInt(normalized.x);
            int y = Mathf.RoundToInt(normalized.y);
            Vector3Int directionInt = new Vector3Int(x, y, 0);

            if (!_isMoveInputHeld)
            {
                // 移動の開始
                _isMoveInputHeld = true;
                RequestStartContinuousMoveServerRpc(directionInt);
            }
            else
            {
                // 移動中の方向転換
                RequestUpdateMoveDirectionServerRpc(directionInt);
            }
        }

        private void HandleMoveCanceled()
        {
            _isMoveInputHeld = false;
            RequestStopContinuousMoveServerRpc();
        }

        private void HandleInteractIntent()
        {
            RequestInteractServerRpc();
        }
        
        [ServerRpc]
        private void RequestSpawnedServerRpc()
        {
            OnPlayerSpawned_Server?.Invoke(OwnerClientId, transform.position);
        }

        [ServerRpc]
        private void RequestStartContinuousMoveServerRpc(Vector3Int moveDirection)
        {
            _continuousMoveDirection_Server = moveDirection;
            if (!_isMoving_Server)
            {
                StartCoroutine(ContinuousMove_Server());
            }
        }

        [ServerRpc]
        private void RequestUpdateMoveDirectionServerRpc(Vector3Int newDirection)
        {
            _continuousMoveDirection_Server = newDirection;
        }

        [ServerRpc]
        private void RequestStopContinuousMoveServerRpc()
        {
            _continuousMoveDirection_Server = Vector3Int.zero;
        }

        private IEnumerator ContinuousMove_Server()
        {
            _isMoving_Server = true;

            while (_continuousMoveDirection_Server != Vector3Int.zero)
            {
                if (_levelService == null || _grid == null || _statusReader == null)
                {
                    _isMoving_Server = false;
                    yield break;
                }

                Vector3Int targetGridPos = NetworkGridPosition.Value + _continuousMoveDirection_Server;

                if (_levelService.IsWalkable(targetGridPos))
                {
                    float moveSpeed = _statusReader.GetStatValue(OwnerClientId, PlayerStat.MoveSpeed);
                    float duration = 1f / Mathf.Max(0.1f, moveSpeed);

                    // 斜め移動の場合、移動時間を√2倍する
                    if (_continuousMoveDirection_Server.x != 0 && _continuousMoveDirection_Server.y != 0)
                    {
                        duration *= 1.414f;
                    }
                    
                    NetworkMoveDuration.Value = duration;
                    NetworkGridPosition.Value = targetGridPos;
                    _currentState.Value = PlayerState.Moving;

                    yield return new WaitForSeconds(duration);

                    transform.position = _grid.GetCellCenterWorld(NetworkGridPosition.Value);
                    OnPlayerMoved_Server?.Invoke(OwnerClientId, transform.position);
                }
                else
                {
                    // 壁にぶつかったら連続移動の意図をリセット（停止）
                    _continuousMoveDirection_Server = Vector3Int.zero;
                }
            }

            _currentState.Value = PlayerState.Roaming;
            _isMoving_Server = false;
        }

        [ServerRpc]
        private void RequestInteractServerRpc()
        {
            Debug.Log("サーバーがクライアントからのインタラクト要求を受信しました。");
        }

        private void OnStateChanged(PlayerState previousValue, PlayerState newValue)
        {
            _stateMachine.ChangeState(newValue);
        }

        private void OnGridPositionChanged(Vector3Int previousValue, Vector3Int newValue)
        {
            if (_stateMachine.CurrentStateEnum == PlayerState.Moving)
            {
                _stateMachine.CurrentIPlayerState?.OnTargetPositionChanged();
            }
        }

        #endregion
    }
}
