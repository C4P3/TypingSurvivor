using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Typing.Builder
{
    /// <summary>
    /// Trie（トライ木）のノード。各ノードが入力可能な1文字を表す。
    /// このクラスはタイピング機能の内部でのみ使用される。
    /// </summary>
    internal class TrieNode
    {
        public readonly char Character;
        public readonly Dictionary<char, TrieNode> Children = new();
        public bool IsEndOfWord = false; // このノードが単語の終わり（ひらがな文字列全体の終わり）か
        public TrieNode Parent;

        public TrieNode(char character, TrieNode parent = null)
        {
            Character = character;
            Parent = parent;
        }
    }

    /// <summary>
    /// ひらがな文字列と変換テーブルから、タイピング用のTrie木を構築するクラス。
    /// </summary>
    internal class TrieBuilder
    {
        private readonly TypingConversionTable _table;
        private readonly KanaParser _parser;
        private List<TrieNode> _leafNodes; // 現在のTrieの末端ノードリスト

        public TrieBuilder(TypingConversionTable table)
        {
            _table = table;
            _parser = new KanaParser(table);
        }

        public TrieNode Build(string hiragana)
        {
            var root = new TrieNode('^');
            _leafNodes = new List<TrieNode> { root };

            var tokens = _parser.Parse(hiragana);

            for (int i = 0; i < tokens.Count; i++)
            {
                var currentToken = tokens[i];
                var nextToken = (i + 1 < tokens.Count) ? tokens[i + 1] : null;

                switch (currentToken.Type)
                {
                    case KanaType.Sokuon:
                        ProcessSokuonToken(nextToken);
                        i++; // 促音は次のトークンも消費するため、インデックスを1つ進める
                        break;
                    case KanaType.Hatsuon:
                        ProcessHatsuonToken(nextToken);
                        break;
                    case KanaType.Normal:
                    case KanaType.Youon:
                        ProcessNormalToken(currentToken);
                        break;
                    case KanaType.Symbol:
                        ProcessSymbolToken(currentToken);
                        break;
                }
            }

            // 全てのひらがなを処理した後、最終ノードに終端マークを付ける
            foreach (var node in _leafNodes)
            {
                node.IsEndOfWord = true;
            }

            return root;
        }

        private void ProcessNormalToken(KanaToken token)
        {
            if (_table.Definitions.TryGetValue(token.Kana, out var romajiPatterns))
            {
                AddNewLeaves(romajiPatterns);
            }
            else
            {
                Debug.LogError($"[TrieBuilder] Conversion definition not found for: {token.Kana}");
            }
        }
        
        private void ProcessSymbolToken(KanaToken token)
        {
            // 記号などは、その文字自体をパターンとして扱う
            AddNewLeaves(new List<string> { token.Kana });
        }

        private void ProcessHatsuonToken(KanaToken nextToken)
        {
            var romajiPatterns = new List<string>();
            var rule = _table.Rules.Hatsuon;

            // 文末、または次が母音・な行・や行などなら "nn"
            if (nextToken == null || rule.DoubleNIfNextIs.Contains(nextToken.Kana))
            {
                romajiPatterns.Add("nn");
            }
            else
            {
                romajiPatterns.Add("n");
            }

            // 別パターン "xn" も追加
            if (!romajiPatterns.Contains(rule.Default))
            {
                romajiPatterns.Add(rule.Default);
            }
            
            AddNewLeaves(romajiPatterns);
        }

        private void ProcessSokuonToken(KanaToken nextToken)
        {
            var rule = _table.Rules.Sokuon;
            var romajiPatterns = new HashSet<string>(); // 重複を避ける

            if (nextToken != null && _table.Definitions.TryGetValue(nextToken.Kana, out var nextRomajiPatterns))
            {
                foreach (var pattern in nextRomajiPatterns)
                {
                    if (pattern.Length > 0)
                    {
                        string consonant = pattern.Substring(0, 1);
                        if (rule.Consonants.TryGetValue(consonant, out var repeatConsonant))
                        {
                            // 繰り返す子音 + 本来のローマ字パターン
                            romajiPatterns.Add(repeatConsonant + pattern);
                        }
                    }
                }
            }

            // パターンが見つからなかった場合、またはデフォルトパターンがまだ追加されていない場合
            if (romajiPatterns.Count == 0 || !romajiPatterns.Contains(rule.Default))
            {
                 romajiPatterns.Add(rule.Default);
            }
            
            AddNewLeaves(romajiPatterns.ToList());
        }

        /// <summary>
        /// 現在の末端ノードリストの各ノードに、新しいローマ字パターンを追加し、
        /// 末端ノードリストを更新する。
        /// </summary>
        private void AddNewLeaves(List<string> patterns)
        {
            var nextLeafNodes = new List<TrieNode>();
            foreach (var node in _leafNodes)
            {
                foreach (var pattern in patterns)
                {
                    var leaf = AddPatternToNode(node, pattern);
                    nextLeafNodes.Add(leaf);
                }
            }
            _leafNodes = nextLeafNodes;
        }

        private TrieNode AddPatternToNode(TrieNode startNode, string pattern)
        {
            var currentNode = startNode;
            foreach (char c in pattern)
            {
                if (!currentNode.Children.TryGetValue(c, out var nextNode))
                {
                    nextNode = new TrieNode(c, currentNode);
                    currentNode.Children[c] = nextNode;
                }
                currentNode = nextNode;
            }
            return currentNode;
        }
    }
}
