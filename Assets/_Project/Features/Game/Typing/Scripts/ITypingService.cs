using System;

namespace TypingSurvivor.Features.Game.Typing
{
    public interface ITypingService
    {
        bool IsTyping { get; }
        int CorrectCharCount { get; }
        int TotalKeyPressCount { get; }

        event System.Action OnTypingProgressed;
        event System.Action OnTypingSuccess;
        event System.Action OnTypingMiss;
        event System.Action OnTypingCancelled;

        void StartTyping();
        void StartTyping(TypingChallenge challenge);
        void CancelTyping();
        void StopTyping();
        void ResetStats();

        string GetTypedRomaji();
        string GetRemainingRomaji();
        string GetDisplayText();
    }
}

