using Unity.Netcode;
using UnityEngine;
using System;

public class GameManager : NetworkBehaviour, IGameStateReader, IGameStateWriter
{
    // --- GameStateに相当するNetworkVariable群 ---
    public NetworkVariable<float> GameTimer { get; } = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> OxygenLevel { get; } = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<PlayerData> PlayerDatas { get; } = new();　// プレイヤーごとのデータはNetworkListで管理

    // --- Readerインターフェースの実装 ---
    public float CurrentOxygen => OxygenLevel.Value;
    public int CurrentScore => 0;
    public event Action<float> OnOxygenChanged;
    public event Action<int> OnScoreChanged;

    // private IGameModeStrategy _gameMode;

    // GameManagerが他のシステムの参照も知っている
    private IPlayerStatusSystemReader _playerStatusSystem;
    private InGameHUDManager _hudManager;

    // [SyncVar]
    // private GameState _gameState;

    void Awake()
    {
        // 自身や他の主要なシステムへの参照をここで確立する
        _playerStatusSystem = GetComponentInChildren<IPlayerStatusSystemReader>(); // 例
        _hudManager = FindFirstObjectByType<InGameHUDManager>();
    }

    void Start()
    {
        // 各Managerに必要な依存性を注入して初期化する
        // thisはIGameStateReaderとして渡される
        _hudManager.Initialize(this, _playerStatusSystem);
    }

    // --- Writerインターフェースの実装 ---
     public void AddOxygen(float amount)
    {
        if (!IsServer) return;
        OxygenLevel.Value = Mathf.Clamp(OxygenLevel.Value + amount, 0, 100);
    }
    
    public void AddScore(ulong clientId, int amount)
    {
        if (!IsServer) return;
        for (int i = 0; i < PlayerDatas.Count; i++)
        {
            if (PlayerDatas[i].ClientId == clientId)
            {
                var data = PlayerDatas[i];
                data.Score += amount;
                PlayerDatas[i] = data; // structなので再設定が必要
                return;
            }
        }
    }
    public void SetGameOver() { }
    
    private void SubscribeServerEvents()
    {
        if (!IsServer) return;
        ILevelService.OnBlockDestroyed_Server += HandleBlockDestroyed;
    }

    private void HandleBlockDestroyed(ulong clientId, Vector3Int position)
    {
        // スコアを加算する処理...
        // _gameState.Score.Value += 10;
    }

    // void Update()
    // {
    //     if () // サーバーだけがゲーム状態を更新
    //     {
    //         _gameState.UpdateOxygen(Time.deltaTime);
    //         if (_gameMode.IsGameOver(_gameState))
    //         {
    //             // ゲームオーバー処理
    //         }
    //     }
    // }
}