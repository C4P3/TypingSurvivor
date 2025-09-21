using Unity.Netcode;
using UnityEngine;

// FacadeはNetworkBehaviourを継承し、ネットワーク通信の起点となる
public class PlayerFacade : NetworkBehaviour
{
    // --- 参照 ---
    [Header("コンポーネント参照")]
    [SerializeField] private PlayerInput _input;
    [SerializeField] private PlayerStateMachine _stateMachine;
    [SerializeField] private PlayerView _view;

    // --- 同期変数 ---
    // NetcodeではNetworkVariable<T>を使って変数を同期する
    // サーバー側でのみ書き込み可能に設定するのが安全
    private readonly NetworkVariable<PlayerState> _currentState = new(writePerm: NetworkVariableWritePermission.Server); // PlayerStateは仮のenum

    #region Unity & Network Callbacks

    /// <summary>
    /// オブジェクトがネットワーク上に出現した時に、サーバー・クライアント両方で呼ばれる
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // NetworkVariableの値が変化した時に呼ばれるメソッドを登録
        _currentState.OnValueChanged += OnStateChanged;

        // IsOwnerは、このオブジェクトの操作権を持つクライアントかどうかを判定する
        if (IsOwner)
        {
            // 自分が操作プレイヤーなら、入力コンポーネントを有効化
            _input.enabled = true;
            _input.OnInteractIntent += HandleInteractIntent;
        }
    }

    /// <summary>
    /// オブジェクトがネットワークから消える時に呼ばれる
    /// </summary>
    public override void OnNetworkDespawn()
    {
        // 登録したイベントは必ず解除する（メモリリーク防止）
        _currentState.OnValueChanged -= OnStateChanged;

        if (IsOwner)
        {
            _input.enabled = false;
            _input.OnInteractIntent -= HandleInteractIntent;
        }
    }

    #endregion

    #region Event Handlers & RPCs

    /// <summary>
    /// [クライアントサイド] Inputからのイベントを受け取る (IsOwner == true のクライアントでのみ)
    /// </summary>
    private void HandleInteractIntent()
    {
        // サーバーに対して「インタラクティブなアクションをしたい」という要求を送信する
        RequestInteractServerRpc();
    }
    
    /// <summary>
    /// [サーバーサイド] クライアントからの要求を受け取り、サーバー上でのみ実行される
    /// </summary>
    [ServerRpc]
    private void RequestInteractServerRpc()
    {
        // このコードブロックは IsServer == true のコンテキストで実行される
        Debug.Log("サーバーがクライアントからのインタラクト要求を受信しました。");

        // サーバー側で「インタラクト可能か？」などのゲームロジックをここに書く
        // 例: タイピングを開始できる状態なら、状態を更新する
        // _currentState.Value = PlayerState.Typing;
    }

    /// <summary>
    // [クライアントサイド] _currentStateの値がサーバー側で変更された時に全クライアントで呼ばれる
    /// </summary>
    private void OnStateChanged(PlayerState previousValue, PlayerState newValue)
    {
        Debug.Log($"State changed from {previousValue} to {newValue} on client {NetworkManager.LocalClientId}");
        
        // 状態の変更をStateMachineとViewに伝達する
        _stateMachine.ChangeState(newValue);
        _view.UpdateAnimation(newValue);
    }

    #endregion
}

// プレイヤーの状態を定義するenum（仮）
public enum PlayerState
{
    Roaming,
    Moving,
    Typing
}

