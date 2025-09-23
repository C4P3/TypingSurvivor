using System;
using Unity.Netcode;
using UnityEngine;
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

        // --- 同期変数 ---
        // private readonly NetworkVariable<PlayerState> _currentState = new(writePerm: NetworkVariableWritePermission.Server);

        // --- 内部状態 ---
        [SerializeField] private float _moveSpeed = 5f; // 仮の移動速度

        #region Unity & Network Callbacks

        public override void OnNetworkSpawn()
        {
            // _currentState.OnValueChanged += OnStateChanged;

            if (IsOwner)
            {
                _input.enabled = true;
                _input.OnMoveIntent += HandleMoveIntent;
                _input.OnInteractIntent += HandleInteractIntent;
                OnPlayerSpawned_Server?.Invoke(OwnerClientId, this.gameObject.transform.position);
            }
        }

        public override void OnNetworkDespawn()
        {
            // _currentState.OnValueChanged -= OnStateChanged;

            if (IsOwner)
            {
                _input.enabled = false;
                _input.OnMoveIntent -= HandleMoveIntent;
                _input.OnInteractIntent -= HandleInteractIntent;
                OnPlayerDespawned_Server?.Invoke(OwnerClientId);
            }
        }

        #endregion

        #region Event Handlers & RPCs

        /// <summary>
        /// [クライアントサイド] Inputからの移動イベントを受け取る
        /// </summary>
        private void HandleMoveIntent(Vector2 moveDirection)
        {
            // サーバーに移動要求を送信する
            RequestMoveServerRpc(moveDirection);
        }

        /// <summary>
        /// [クライアントサイド] Inputからのインタラクトイベントを受け取る
        /// </summary>
        private void HandleInteractIntent()
        {
            RequestInteractServerRpc();
        }
        
        /// <summary>
        /// [サーバーサイド] クライアントからの移動要求を受け取り、サーバー上でのみ実行される
        /// </summary>
        [ServerRpc]
        private void RequestMoveServerRpc(Vector2 moveDirection)
        {
            // サーバー側で移動処理を実行
            transform.Translate(new Vector3(moveDirection.x, moveDirection.y, 0) * _moveSpeed * Time.deltaTime);
            
            // TODO: OnPlayerMoved_ServerのInvokeを設定
        }

        /// <summary>
        /// [サーバーサイド] クライアントからのインタラクト要求を受け取り、サーバー上でのみ実行される
        /// </summary>
        [ServerRpc]
        private void RequestInteractServerRpc()
        {
            Debug.Log("サーバーがクライアントからのインタラクト要求を受信しました。");
            // _currentState.Value = PlayerState.Typing;
        }

        // private void OnStateChanged(PlayerState previousValue, PlayerState newValue)
        // {
        //     Debug.Log($"State changed from {previousValue} to {newValue} on client {NetworkManager.LocalClientId}");
        //     _stateMachine.ChangeState(newValue);
        //     _view.UpdateAnimation(newValue);
        // }

        #endregion
    }
}
