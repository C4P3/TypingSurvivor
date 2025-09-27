using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using TypingSurvivor.Features.Game.Player.Input;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Level;


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

        // --- クライアントサイドのシステム ---
        private ITypingService _typingService;

        // --- サーバーサイドの依存関係 ---
        private ILevelService _levelService;
        private IItemService _itemService;
        private IPlayerStatusSystemReader _statusReader;
        private Grid _grid;

        // --- 同期変数 ---
        private readonly NetworkVariable<PlayerState> _currentState = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<Vector3Int> NetworkGridPosition = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> NetworkMoveDuration = new(0.25f, writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<Vector3Int> NetworkTypingTargetPosition = new(writePerm: NetworkVariableWritePermission.Server);

        // --- サーバーサイドの内部状態 ---
        private Vector3Int _continuousMoveDirection_Server;
        private bool _isMoving_Server;

        #region Properties for States
        public Grid Grid => _grid;
        public float MoveDuration => NetworkMoveDuration.Value;
        public PlayerInput PlayerInput => _input;
        public ITypingService TypingService => _typingService;
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

            var serviceLocator = AppManager.Instance;
            if (serviceLocator == null)
            {
                Debug.LogError("AppManager instance not found!");
                return;
            }

            if (IsServer)
            {
                _levelService = serviceLocator.GetService<ILevelService>();
                _itemService = serviceLocator.GetService<IItemService>();
                _statusReader = serviceLocator.StatusReader; // Coreサービスは直接参照可能
                if (_levelService == null) Debug.LogError("ILevelServiceの実装が見つかりません。");
                if (_itemService == null) Debug.LogError("IItemServiceの実装が見つかりません。");
                if (_statusReader == null) Debug.LogError("IPlayerStatusSystemReaderの実装が見つかりません。");

                _currentState.Value = PlayerState.Roaming;
            }

            if (IsOwner)
            {
                _input.enabled = true;
                _input.OnMovePerformed += HandleMovePerformed;
                _input.OnMoveCanceled += HandleMoveCanceled;

                _typingService = serviceLocator.GetService<ITypingService>();
                if (_typingService != null)
                {
                    _typingService.OnTypingSuccess += HandleTypingSuccess;
                }
                else
                {
                    Debug.LogError("ITypingServiceの実装が見つかりません。");
                }
                
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

                _typingService.OnTypingSuccess -= HandleTypingSuccess;
                _typingService.StopTyping(); // 念のため購読解除
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
            float absX = Mathf.Abs(normalized.x);
            float absY = Mathf.Abs(normalized.y);

            int x, y;

            if (absX > absY)
            {
                x = (int)Mathf.Sign(normalized.x);
                y = 0;
            }
            else
            {
                x = 0;
                y = (int)Mathf.Sign(normalized.y);
            }
            
            Vector3Int directionInt = new Vector3Int(x, y, 0);

            RequestMoveBasedOnStateServerRpc(directionInt);
        }

        private void HandleMoveCanceled()
        {
            RequestStopContinuousMoveServerRpc();
        }

        private void HandleTypingSuccess()
        {
            // タイピング成功をサーバーに通知し、ブロック破壊を要求する
            DestroyBlock_ServerRpc(NetworkTypingTargetPosition.Value);
        }
        
        [ServerRpc]
        private void RequestSpawnedServerRpc()
        {
            OnPlayerSpawned_Server?.Invoke(OwnerClientId, transform.position);
        }

        [ServerRpc]
        private void RequestMoveBasedOnStateServerRpc(Vector3Int direction)
        {
            switch (_currentState.Value)
            {
                case PlayerState.Roaming:
                case PlayerState.Moving:
                    HandleMoveIntent_Server(direction);
                    break;
                
                case PlayerState.Typing:
                    Vector3Int typingTargetDirection = NetworkTypingTargetPosition.Value - NetworkGridPosition.Value;
                    if (direction != typingTargetDirection)
                    {
                        _currentState.Value = PlayerState.Roaming;
                        HandleMoveIntent_Server(direction);
                    }
                    break;
            }
        }

        private void HandleMoveIntent_Server(Vector3Int direction)
        {
            _continuousMoveDirection_Server = direction;
            if (!_isMoving_Server)
            {
                StartCoroutine(ContinuousMove_Server());
            }
        }

        [ServerRpc]
        private void RequestStopContinuousMoveServerRpc()
        {
            _continuousMoveDirection_Server = Vector3Int.zero;
        }

        [ServerRpc]
        private void DestroyBlock_ServerRpc(Vector3Int blockPosition)
        {
            // TODO: 破壊権限のチェックなど
            
            _levelService?.DestroyBlock(OwnerClientId, blockPosition);
            
            // 破壊後はRoaming状態に戻る
            _currentState.Value = PlayerState.Roaming;
        }

        private IEnumerator ContinuousMove_Server()
        {
            _isMoving_Server = true;

            while (_continuousMoveDirection_Server != Vector3Int.zero)
            {
                if (_levelService == null || _grid == null || _statusReader == null || _itemService == null)
                {
                    _isMoving_Server = false;
                    yield break;
                }

                Vector3Int targetGridPos = NetworkGridPosition.Value + _continuousMoveDirection_Server;

                // 移動先にアイテムがあれば取得する
                if (_levelService.HasItemTile(targetGridPos))
                {
                    _itemService.AcquireItem(OwnerClientId, targetGridPos);
                }

                if (_levelService.IsWalkable(targetGridPos))
                {
                    float moveSpeed = _statusReader.GetStatValue(OwnerClientId, PlayerStat.MoveSpeed);
                    float duration = 1f / Mathf.Max(0.1f, moveSpeed);
                    
                    NetworkMoveDuration.Value = duration;
                    NetworkGridPosition.Value = targetGridPos;
                    _currentState.Value = PlayerState.Moving;

                    yield return new WaitForSeconds(duration);

                    transform.position = _grid.GetCellCenterWorld(NetworkGridPosition.Value);
                    OnPlayerMoved_Server?.Invoke(OwnerClientId, transform.position);
                }
                else
                {
                    // Typingステートに移行し、移動コルーチンを完全に終了させる
                    NetworkTypingTargetPosition.Value = targetGridPos;
                    _currentState.Value = PlayerState.Typing;
                    _continuousMoveDirection_Server = Vector3Int.zero;
                    _isMoving_Server = false;
                    yield break;
                }
            }

            _currentState.Value = PlayerState.Roaming;
            _isMoving_Server = false;
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

        #region Public Server-Side Methods
        /// <summary>
        /// Respawns the player at a new position. Must be called on the server.
        /// </summary>
        public void RespawnAt(Vector3 newWorldPosition)
        {
            if (!IsServer) return;

            transform.position = newWorldPosition;
            NetworkGridPosition.Value = _grid.WorldToCell(newWorldPosition);
            
            // Reset server-side state
            _currentState.Value = PlayerState.Roaming;
            _continuousMoveDirection_Server = Vector3Int.zero;
            _isMoving_Server = false;
        }
        #endregion
    }
}
