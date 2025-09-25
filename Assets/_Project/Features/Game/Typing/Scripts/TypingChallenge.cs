using System.Collections.Generic;
using System.Text;
using TypingSurvivor.Features.Game.Typing.Builder;

namespace TypingSurvivor.Features.Game.Typing
{
    /// <summary>
    /// 1回ごとのタイピングのお題（単語やフレーズ）を管理するクラス。
    /// 日本語の原文、ひらがな、そして正解となるローマ字パターンのTrie（トライ木）を保持する。
    /// </summary>
    public class TypingChallenge
    {
        public string OriginalText { get; private set; }
        public string Hiragana { get; private set; }

        // Trieの内部構造はTypingChallengeの外部に公開しない
        private readonly TrieNode _root;
        private TrieNode _currentNode;
        
        public TypingChallenge(string originalText, string hiragana, TypingConversionTable conversionTable)
        {
            OriginalText = originalText;
            Hiragana = hiragana;

            var builder = new TrieBuilder(conversionTable);
            _root = builder.Build(hiragana);
            _currentNode = _root;
        }

        public TypeResult ProcessInput(char inputChar)
        {
            if (_currentNode.Children.TryGetValue(inputChar, out var nextNode))
            {
                _currentNode = nextNode;
                // IsEndOfWordフラグは、ひらがな文字列全体の入力が完了したことを示す
                if (_currentNode.IsEndOfWord)
                {
                    return TypeResult.Finished;
                }
                return TypeResult.Correct;
            }
            return TypeResult.Incorrect;
        }

        public string GetTypedRomaji()
        {
            var builder = new StringBuilder();
            var node = _currentNode;
            while (node != null && node.Parent != null)
            {
                builder.Insert(0, node.Character);
                node = node.Parent;
            }

            return builder.ToString();
        }

        public string GetRemainingRomaji()
        {
            if (_currentNode.IsEndOfWord) return "";

            // 幅優先探索(BFS)で最短経路を見つける
            var queue = new Queue<TrieNode>();
            var visited = new Dictionary<TrieNode, TrieNode>(); // Key: To, Value: From

            queue.Enqueue(_currentNode);
            visited[_currentNode] = null;

            TrieNode destination = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.IsEndOfWord)
                {
                    destination = current;
                    break; // 最短経路が見つかった
                }

                foreach (var child in current.Children.Values)
                {
                    if (!visited.ContainsKey(child))
                    {
                        visited[child] = current;
                        queue.Enqueue(child);
                    }
                }
            }

            // 経路を再構築
            if (destination != null)
            {
                var path = new StringBuilder();
                var current = destination;
                while (visited.ContainsKey(current) && visited[current] != null && visited[current] != _currentNode)
                {
                    path.Insert(0, current.Character);
                    current = visited[current];
                }
                path.Insert(0, current.Character);
                return path.ToString();
            }

            return ""; // 経路が見つからない場合 (通常はありえない)
        }
    }

    public enum TypeResult
    {
        Correct,
        Incorrect,
        Finished
    }
}

