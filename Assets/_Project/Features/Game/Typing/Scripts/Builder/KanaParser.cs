using System.Collections.Generic;

namespace TypingSurvivor.Features.Game.Typing.Builder
{
    /// <summary>
    /// ひらがな文字列を解析し、意味のある単位(トークン)のリストに変換するクラス。
    /// </summary>
    public class KanaParser
    {
        private readonly TypingConversionTable _conversionTable;

        public KanaParser(TypingConversionTable conversionTable)
        {
            _conversionTable = conversionTable;
        }

        public List<KanaToken> Parse(string hiragana)
        {
            var tokens = new List<KanaToken>();
            if (string.IsNullOrEmpty(hiragana)) return tokens;

            int index = 0;
            while (index < hiragana.Length)
            {
                string sokuonChar = _conversionTable.Rules.Sokuon.Character;
                string hatsuonChar = _conversionTable.Rules.Hatsuon.Character;
                string currentChar = hiragana.Substring(index, 1);

                // 1. 促音チェック
                if (currentChar == sokuonChar)
                {
                    tokens.Add(new KanaToken(sokuonChar, KanaType.Sokuon));
                    index++;
                    continue;
                }

                // 2. 撥音チェック
                if (currentChar == hatsuonChar)
                {
                    tokens.Add(new KanaToken(hatsuonChar, KanaType.Hatsuon));
                    index++;
                    continue;
                }

                // 3. 拗音チェック (2文字)
                if (index + 1 < hiragana.Length)
                {
                    string twoChars = hiragana.Substring(index, 2);
                    if (_conversionTable.Definitions.ContainsKey(twoChars))
                    {
                        tokens.Add(new KanaToken(twoChars, KanaType.Youon));
                        index += 2;
                        continue;
                    }
                }

                // 4. 通常のかな (1文字)
                if (_conversionTable.Definitions.ContainsKey(currentChar))
                {
                    tokens.Add(new KanaToken(currentChar, KanaType.Normal));
                }
                else
                {
                    // 変換テーブルにない文字は記号として扱う
                    tokens.Add(new KanaToken(currentChar, KanaType.Symbol));
                }
                index++;
            }
            return tokens;
        }
    }
}
