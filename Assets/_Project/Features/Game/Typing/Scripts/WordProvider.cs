using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Typing
{
    /// <summary>
    /// CSVファイルから単語リストを読み込み、言語に応じた変換テーブルを使ってお題を提供するクラス。
    /// </summary>
    public class WordProvider : IWordProvider
    {
        private readonly List<WordData> _wordDatabase = new();
        private readonly Dictionary<string, TypingConversionTable> _conversionTables;

        public WordProvider(string csvText, Dictionary<string, TypingConversionTable> conversionTables)
        {
            _conversionTables = conversionTables;
            ParseCsv(csvText);
        }

        private void ParseCsv(string csvText)
        {
            if (string.IsNullOrEmpty(csvText))
            {
                Debug.LogError("CSV text is null or empty.");
                return;
            }

            using (var reader = new StringReader(csvText))
            {
                // ヘッダー行を読み飛ばす
                reader.ReadLine();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(',');
                    if (values.Length >= 4)
                    {
                        string displayText = values[0];
                        string hiraganaText = values[1];
                        if (int.TryParse(values[2], out int level))
                        {
                            string language = values[3].Trim(); // Trim to remove whitespace
                            if (_conversionTables.ContainsKey(language))
                            {
                                _wordDatabase.Add(new WordData(displayText, hiraganaText, level, language));
                            }
                            else
                            {
                                Debug.LogWarning($"Conversion table for language '{language}' not found. Skipping word: {displayText}");
                            }
                        }
                    }
                }
            }
        }

        public TypingChallenge GetNextChallenge()
        {
            if (_wordDatabase.Count == 0)
            {
                Debug.LogError("Word database is empty or no valid words were found.");
                // フォールバックとしてデフォルトのお題を返す (jaテーブルが存在すると仮定)
                if (_conversionTables.TryGetValue("ja", out var fallbackTable))
                {
                    return new TypingChallenge("えらー", "えらー", fallbackTable);
                }
                // それもなければ、チャレンジを返せない
                return null; 
            }

            // ランダムに単語を選択
            WordData wordData = _wordDatabase[Random.Range(0, _wordDatabase.Count)];
            
            // 言語に合った変換テーブルを取得
            TypingConversionTable table = _conversionTables[wordData.Language];
            
            return new TypingChallenge(wordData.DisplayText, wordData.HiraganaText, table);
        }
    }
}
