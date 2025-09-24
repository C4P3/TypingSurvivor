using System;
using UnityEngine.InputSystem;

// TODO: TypingChallengeクラスが実装されたら、これと差し替える
public struct TypingChallenge
{
    public string Word; // e.g., "neko"
}

public class TypingManager
{
    public event Action OnTypingSuccess;
    
    private TypingChallenge _currentChallenge;
    private string _remainingText;

    public void StartTyping(TypingChallenge challenge)
    {
        _currentChallenge = challenge;
        _remainingText = _currentChallenge.Word;
        
        // テキスト入力イベントを購読
        Keyboard.current.onTextInput += OnTextInput;
    }

    public void StopTyping()
    {
        // テキスト入力イベントの購読を解除
        Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnTextInput(char character)
    {
        if (string.IsNullOrEmpty(_remainingText)) return;

        // 入力された文字が、残りのテキストの先頭と一致するかチェック
        if (character == _remainingText[0])
        {
            _remainingText = _remainingText.Substring(1);

            // 全て入力し終えたかチェック
            if (string.IsNullOrEmpty(_remainingText))
            {
                OnTypingSuccess?.Invoke();
                StopTyping();
            }
        }
    }
}