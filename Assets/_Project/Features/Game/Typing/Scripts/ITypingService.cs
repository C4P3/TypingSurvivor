using System;

namespace TypingSurvivor.Features.Game.Typing
{
    /// <summary>
    /// クライアントサイドのタイピング機能を提供するサービスインターフェース。
    /// </summary>
    public interface ITypingService
    {
        event Action OnTypingSuccess;
        event Action OnTypingMiss;
        event Action OnTypingCancelled;
        event Action OnTypingProgressed;

        bool IsTyping { get; }

        void StartTyping();
        void StartTyping(TypingChallenge challenge);
        void CancelTyping();
        void StopTyping();

        string GetOriginalText();
        string GetTypedRomaji();
        string GetRemainingRomaji();
    }
}
