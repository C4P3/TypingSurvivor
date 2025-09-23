using UnityEngine;
using Unity.Netcode;

namespace TypingSurvivor.Features.Game.Gameplay
{
    /// <summary>
    /// Initializes the core gameplay systems in the scene.
    /// This class is responsible for wiring up dependencies (Manual DI).
    /// </summary>
    public class GameSceneContext : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private GameManager _gameManager;
        
        // 将来的に、シングルプレイかマルチプレイかをここで切り替える
        // [SerializeField] private IGameModeStrategy _gameModeStrategy;

        private void Awake()
        {
            // TODO: IGameModeStrategyをどうにかして生成・取得する
            // 例えば、シングルの場合は new SinglePlayerStrategy() のように。
            IGameModeStrategy strategy = new SinglePlayerStrategy(); // 仮実装

            // GameManagerに必要な依存性を注入する
            _gameManager.Initialize(_gameState, strategy);

            // UIなど、他のシステムにも依存性を注入していく
            // InGameHUDManager hudManager = FindObjectOfType<InGameHUDManager>();
            // hudManager.Initialize(_gameState); // IGameStateReaderを渡す
        }
    }
}
