using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;

public class GameManager : NetworkBehaviour, IGameStateReader, IGameStateWriter
{
    #region NetworkVariables
    public NetworkVariable<GamePhase> CurrentPhase { get; } = new(GamePhase.WaitingForPlayers);
    public NetworkVariable<float> GameTimer { get; } = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> OxygenLevel { get; } = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<PlayerData> PlayerDatas { get; } = new(); // プレイヤーごとのデータはNetworkListで管理
    #endregion

    // --- Readerインターフェースの実装 ---
    public float CurrentOxygen => OxygenLevel.Value;
    public event Action<float> OnOxygenChanged;
    public event Action<int> OnScoreChanged;

    // 
    float oxygenDecreaseRate = 0.9f;

    // GameManagerが他のシステムの参照も知っている
    private ILevelService _levelService;
    private IPlayerStatusSystemReader _playerStatusSystem;
    private InGameHUDManager _hudManager;
    private IGameModeStrategy _gameModeStrategy;

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

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // クライアントは、フェーズの変更を監視する
            CurrentPhase.OnValueChanged += HandlePhaseChanged_Client;
        }
        if (IsServer)
        {
            // TODO: ここでサーバー側の初期化処理を開始する
            // StartCoroutine(ServerGameLoop());
        }
    }


    // --- Readerインターフェースの実装
    public int GetPlayerScore(ulong clientId)
    {
        foreach (PlayerData playerdata in PlayerDatas)
        {
            if (playerdata.ClientId == clientId)
            {
                return playerdata.Score;
            }
        }
        // 見つからなかった場合
        return 0;
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
    public void SetPlayerGameOver(ulong clientId)
    {

    }

    private void SubscribeServerEvents()
    {
        if (!IsServer) return;
        _levelService.OnBlockDestroyed_Server += HandleBlockDestroyed;
    }

    [ServerRpc]
    private IEnumerator ServerGameLoop()
    {
        // --- 待機フェーズ ---
        CurrentPhase.Value = GamePhase.WaitingForPlayers;
        while (NetworkManager.Singleton.ConnectedClients.Count < _gameModeStrategy.PlayerCount)
        {
            yield return null;
        }

        // --- カウントダウンフェーズ ---
        CurrentPhase.Value = GamePhase.Countdown;
        // NetworkVariable<float> CountdownTimer を用意して同期しても良い
        yield return new WaitForSeconds(5);

        // --- プレイフェーズ ---
        CurrentPhase.Value = GamePhase.Playing;
        while (true)
        {
            // 酸素の自然減少
            OxygenLevel.Value -= oxygenDecreaseRate * Time.deltaTime;
            
            // ★ルールブック（Strategy）に終了条件を確認してもらう
            if (_gameModeStrategy.IsGameOver(this))
            {
                break; // ゲーム終了
            }
            
            yield return null;
        }

        // --- 終了フェーズ ---
        CurrentPhase.Value = GamePhase.Finished;
        GameResult result = _gameModeStrategy.CalculateResult(this);
        // TODO: リザルト情報をクライアントに通知し、スコアを送信する
    }

    private void HandleBlockDestroyed(ulong clientId, Vector3Int position)
    {
        // スコアを加算する処理...
        // _gameState.Score.Value += 10;
    }
    
    // クライアント側でフェーズ変更を検知した時の処理
    private void HandlePhaseChanged_Client(GamePhase previousPhase, GamePhase newPhase)
    {
        Debug.Log($"Game phase changed to: {newPhase}");
        // 例: UIにカウントダウン表示を出す、プレイヤーの入力を有効/無効にするなど
        // UIManager.Instance.OnGamePhaseChanged(newPhase);
        // PlayerInputManager.SetInputActive(newPhase == GamePhase.Playing);
    }
}