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
        public readonly NetworkVariable<Vector3Int> NetworkTypingTargetPosition = new(writePerm: NetworkVariableWritePermission.Server);

        // --- クライアントサイドの内部状態 ---
        private bool _isMoveInputHeld;

        // --- サーバーサイドの内部状態 ---
        private Vector3Int _continuousMoveDirection_Server;
        private bool _isMoving_Server;

        #region Properties for States
        public Grid Grid => _grid;
        public float MoveDuration => NetworkMoveDuration.Value;
        public PlayerInput PlayerInput => _input;
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
                _input.OnTypingMovePerformed += HandleTypingMovePerformed;
                
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
                _input.OnTypingMovePerformed -= HandleTypingMovePerformed;
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
                new TypingState(this)
            };
            _stateMachine = new PlayerStateMachine(states);
        }

        #region Event Handlers & RPCs

        private void HandleMovePerformed(Vector2 moveDirection)
        {
            Vector2 normalized = moveDirection.normalized;
            int x = Mathf.RoundToInt(normalized.x);
            int y = Mathf.RoundToInt(normalized.y);
            Vector3Int directionInt = new Vector3Int(x, y, 0);

            if (!_isMoveInputHeld)
            {
                _isMoveInputHeld = true;
                RequestStartContinuousMoveServerRpc(directionInt);
            }
            else
            {
                RequestUpdateMoveDirectionServerRpc(directionInt);
            }
        }

        private void HandleMoveCanceled()
        {
            if (!_isMoveInputHeld) return;
            _isMoveInputHeld = false;
            RequestStopContinuousMoveServerRpc();
        }

        private void HandleTypingMovePerformed(Vector2 moveDirection)
        {
            RequestExitTypingStateServerRpc();
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

        [ServerRpc]
        private void RequestExitTypingStateServerRpc()
        {
            if (_currentState.Value == PlayerState.Typing)
            {
                _currentState.Value = PlayerState.Roaming;
            }
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
                    // 壁にぶつかったのでタイピングステートに移行
                    NetworkTypingTargetPosition.Value = targetGridPos;
                    _currentState.Value = PlayerState.Typing;
                    _continuousMoveDirection_Server = Vector3Int.zero; // 移動の意図をリセット
                }
            }

            _currentState.Value = PlayerState.Roaming;
            _isMoving_Server = false;
        }

        private void OnStateChanged(PlayerState previousValue, PlayerState newValue)
        {
            // サーバー側で移動が完了してRoamingに戻った場合、クライアント側の入力がまだ続いている可能性があるため、
            // クライアントの入力状態をリセットする
            if (IsOwner && newValue == PlayerState.Roaming)
            {
                _isMoveInputHeld = false;
            }
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
