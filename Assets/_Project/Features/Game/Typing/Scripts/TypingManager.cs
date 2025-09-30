using System;
using UnityEngine.InputSystem;

namespace TypingSurvivor.Features.Game.Typing
{
    /// <summary>
    /// タイピング機能の全体的な管理を行うクラス。
    /// InputSystemからの入力を受け取り、現在のタイピングチャレンジ（_currentChallenge）に処理を委譲する。
    /// </summary>
    public class TypingManager : ITypingService
    {
        public event Action OnTypingSuccess;
        public event Action OnTypingMiss;
        public event Action OnTypingCancelled;
        public event Action OnTypingProgressed; // UI更新用のイベント

        private readonly IWordProvider _wordProvider;
        private TypingChallenge _currentChallenge;

        public bool IsTyping => _currentChallenge != null;

        public TypingManager(IWordProvider wordProvider)
        {
            _wordProvider = wordProvider;
        }

        /// <summary>
        /// IWordProviderから新しいお題を取得してタイピングを開始する。
        /// </summary>
        public void StartTyping()
        {
            if (_wordProvider == null)
            {
                UnityEngine.Debug.LogError("WordProvider is not set.");
                return;
            }
            var newChallenge = _wordProvider.GetNextChallenge();
            StartTyping(newChallenge);
        }

        /// <summary>
        /// 指定されたお題でタイピングを開始する。
        /// </summary>
        public void StartTyping(TypingChallenge challenge)
        {
            if (challenge == null)
            {
                UnityEngine.Debug.LogWarning("Tried to start typing with a null challenge.");
                return;
            }
            _currentChallenge = challenge;
            
            // テキスト入力イベントを購読
            Keyboard.current.onTextInput += OnTextInput;
            
            // UIを初期状態に更新
            OnTypingProgressed?.Invoke();
        }

        public void CancelTyping()
        {
            if (!IsTyping) return;
            
            StopTyping();
            OnTypingCancelled?.Invoke();
        }

        public void StopTyping()
        {
            // テキスト入力イベントの購読を解除
            Keyboard.current.onTextInput -= OnTextInput;
            _currentChallenge = null;
        }

        private void OnTextInput(char character)
        {
            if (!IsTyping) return;

            // 入力処理をTypingChallengeに委譲
            var result = _currentChallenge.ProcessInput(character);

            switch (result)
            {
                case TypeResult.Correct:
                    OnTypingProgressed?.Invoke();
                    break;
                
                case TypeResult.Incorrect:
                    OnTypingMiss?.Invoke();
                    break;
                
                case TypeResult.Finished:
                    OnTypingProgressed?.Invoke(); // 最後の文字が入力されたことをUIに反映
                    OnTypingSuccess?.Invoke();
                    StopTyping();
                    break;
            }
        }

        // UI表示用の情報を取得するためのゲッター
        public string GetOriginalText() => _currentChallenge?.OriginalText ?? "";
        public string GetTypedRomaji() => _currentChallenge?.GetTypedRomaji() ?? "";
        public string GetRemainingRomaji() => _currentChallenge?.GetRemainingRomaji() ?? "";
    }
}