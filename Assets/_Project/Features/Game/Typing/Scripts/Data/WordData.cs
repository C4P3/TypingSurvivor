namespace TypingSurvivor.Features.Game.Typing
{
    /// <summary>
    /// CSVから読み込んだタイピングワード1行分のデータを保持する構造体。
    /// </summary>
    public struct WordData
    {
        public readonly string DisplayText;
        public readonly string HiraganaText;
        public readonly int Level;
        public readonly string Language;

        public WordData(string displayText, string hiraganaText, int level, string language)
        {
            DisplayText = displayText;
            HiraganaText = hiraganaText;
            Level = level;
            Language = language;
        }
    }
}
