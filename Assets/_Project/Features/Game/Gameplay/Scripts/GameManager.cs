using Unity.Netcode;
using System;

public class GameManager : NetworkBehaviour, IGameStateReader, IGameStateWriter
{
    // ゲーム開始時にDIコンテナやNetworkManagerから設定される
    public float CurrentOxygen { get; private set; }
    public int CurrentScore { get; private set; }
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
    
    public void AddOxygen(float amount){}
    public void AddScore(int amount){}
    public void SetGameOver(){}

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