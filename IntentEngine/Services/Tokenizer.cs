using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IntentEngine.Services
{
    public class Tokenizer
    {
        private readonly Dictionary<string, int> _vocab;
        private readonly int _maxLen;
        private readonly string _unkToken = "[UNK]";
        private readonly string _clsToken = "[CLS]";
        private readonly string _sepToken = "[SEP]";
        private readonly string _padToken = "[PAD]";

        public int VocabSize { get { return _vocab.Count; } }
        public int MaxLen { get { return _maxLen; } }

        public Tokenizer(int maxLen = 128)
        {
            _vocab = new Dictionary<string, int>();
            _maxLen = maxLen;
            BuildBasicVocab();
        }

        public void LoadVocab(string vocabPath)
        {
            if (!File.Exists(vocabPath))
            {
                return;
            }

            _vocab.Clear();
            var lines = File.ReadAllLines(vocabPath);
            for (int i = 0; i < lines.Length; i++)
            {
                var token = lines[i].Trim();
                if (!string.IsNullOrEmpty(token))
                    _vocab[token] = i;
            }
        }

        public class EncodeResult
        {
            public long[] InputIds { get; set; }
            public long[] AttentionMask { get; set; }
            public long[] TokenTypeIds { get; set; }
        }

        public EncodeResult Encode(string text)
        {
            var tokens = new List<string>();
            tokens.Add(_clsToken);

            var pieces = Tokenize(text);
            tokens.AddRange(pieces);

            tokens.Add(_sepToken);

            if (tokens.Count > _maxLen)
            {
                tokens = tokens.Take(_maxLen - 1).ToList();
                tokens.Add(_sepToken);
            }

            int seqLen = tokens.Count;
            var inputIds = new long[_maxLen];
            var attentionMask = new long[_maxLen];
            var tokenTypeIds = new long[_maxLen];

            for (int i = 0; i < _maxLen; i++)
            {
                if (i < seqLen)
                {
                    inputIds[i] = GetId(tokens[i]);
                    attentionMask[i] = 1;
                }
                else
                {
                    inputIds[i] = GetId(_padToken);
                    attentionMask[i] = 0;
                }
                tokenTypeIds[i] = 0;
            }

            return new EncodeResult
            {
                InputIds = inputIds,
                AttentionMask = attentionMask,
                TokenTypeIds = tokenTypeIds
            };
        }

        private List<string> Tokenize(string text)
        {
            var tokens = new List<string>();

            var parts = Regex.Matches(text ?? "", @"[\w]+|[^\w\s]")
                .Cast<Match>()
                .Select(m => m.Value);

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.Length == 1 && IsChineseChar(part[0]))
                {
                    tokens.Add(part);
                    continue;
                }

                var subTokens = WordPieceTokenize(part.ToLowerInvariant());
                tokens.AddRange(subTokens);
            }

            return tokens;
        }

        private List<string> WordPieceTokenize(string word)
        {
            var tokens = new List<string>();
            if (_vocab.ContainsKey(word))
            {
                tokens.Add(word);
                return tokens;
            }

            int start = 0;
            while (start < word.Length)
            {
                int end = word.Length;
                string sub = null;

                while (end > start)
                {
                    var piece = (start == 0) ? word.Substring(start, end - start)
                                             : "##" + word.Substring(start, end - start);
                    if (_vocab.ContainsKey(piece))
                    {
                        sub = piece;
                        break;
                    }
                    end--;
                }

                if (sub == null)
                {
                    tokens.Add(_unkToken);
                    break;
                }

                tokens.Add(sub);
                start = end;
            }

            return tokens;
        }

        private int GetId(string token)
        {
            int id;
            if (_vocab.TryGetValue(token, out id))
                return id;
            if (_vocab.TryGetValue(_unkToken, out id))
                return id;
            return 100;
        }

        private bool IsChineseChar(char c)
        {
            return c >= 0x4E00 && c <= 0x9FFF;
        }

        private void BuildBasicVocab()
        {
            _vocab["[PAD]"] = 0;
            _vocab["[UNK]"] = 100;
            _vocab["[CLS]"] = 101;
            _vocab["[SEP]"] = 102;
            _vocab["[MASK]"] = 103;

            for (int i = 0x4E00; i <= 0x9FFF; i++)
            {
                var ch = ((char)i).ToString();
                if (!_vocab.ContainsKey(ch))
                    _vocab[ch] = _vocab.Count;
            }

            for (char c = 'a'; c <= 'z'; c++)
                if (!_vocab.ContainsKey(c.ToString()))
                    _vocab[c.ToString()] = _vocab.Count;
            for (char c = '0'; c <= '9'; c++)
                if (!_vocab.ContainsKey(c.ToString()))
                    _vocab[c.ToString()] = _vocab.Count;
        }
    }
}
