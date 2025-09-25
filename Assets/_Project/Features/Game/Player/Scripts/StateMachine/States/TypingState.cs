using UnityEngine;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Core.App; // 新しいTypingManagerのnamespace

namespace TypingSurvivor.Features.Game.Player
{
    public class TypingState : IPlayerState
    {
        private readonly PlayerFacade _facade;

        public TypingState(PlayerFacade playerFacade)
        {
            _facade = playerFacade;
        }

        public void Enter(PlayerState stateFrom)
        {
            Debug.Log("Entering Typing State");
            // TODO: 本来はWordProviderのようなクラスからお題を取得する
            // 現状はTypingChallengeを直接生成する仮実装
            var tempConversionTable = AppManager.Instance.gameConfig.TypingConversionTable.Table; // 仮の変換テーブル
            var challenge = new TypingChallenge("てすと", "てすと", tempConversionTable);
            _facade.TypingService?.StartTyping(challenge);
        }

        public void Execute()
        {
            // TypingManagerが入力処理を行うため、ここでは何もしない
        }

        public void Exit(PlayerState stateTo)
        {
            Debug.Log("Exiting Typing State");
            _facade.TypingService?.StopTyping();
        }

        public void OnTargetPositionChanged()
        {
            // Typing状態では何もしない
        }
    }
}
