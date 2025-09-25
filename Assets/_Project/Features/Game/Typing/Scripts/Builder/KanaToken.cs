namespace TypingSurvivor.Features.Game.Typing.Builder
{
    public enum KanaType
    {
        Normal,    // 通常のかな (e.g., "か", "き")
        Youon,     // 拗音 (e.g., "きゃ", "しょ")
        Sokuon,    // 促音 ("っ")
        Hatsuon,   // 撥音 ("ん")
        Symbol     // 記号など、変換テーブルにない文字
    }

    public class KanaToken
    {
        public string Kana { get; }
        public KanaType Type { get; }

        public KanaToken(string kana, KanaType type)
        {
            Kana = kana;
            Type = type;
        }
    }
}
