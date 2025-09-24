using System;
using GameControlsInput;
using UnityEngine.InputSystem;

public struct TypingText
{
    public string Title;    // 表示用の日本語テキスト
    public string Hiragana; // 判定用のひらがな
    public int Level;       // 難易度レベル
}

public class TypingManager
{
    // public static event Action OnTypingEnded;
    
    void OnEnable()
    {
        // テキスト入力イベントを購読
        Keyboard.current.onTextInput += OnTextInput;
    }

    void OnDisable()
    {
        Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnTextInput(char character)
    {
        // ここでタイピング判定ロジックを呼び出す
        // 例: _typingModel.TypeCharacter(character);
    }
}