using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    // ゲーム開始時にDIコンテナやNetworkManagerから設定される
    // private IGameModeStrategy _gameMode;

    // [SyncVar]
    // private GameState _gameState;

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