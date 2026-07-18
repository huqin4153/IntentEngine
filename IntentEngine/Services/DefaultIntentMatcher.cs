using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using IntentEngine.Contracts;
using IntentEngine.Models;

namespace IntentEngine.Services
{
    public class DefaultIntentMatcher : IIntentMatcher
    {
        private readonly IEmbeddingService _embedding;
        private readonly IIntentRepository _intentRepo;

        private List<Intent> _intents;
        private readonly ConcurrentDictionary<int, float[]> _vectors = new ConcurrentDictionary<int, float[]>();
        private bool _vectorsBuilt = false;

        public event EventHandler<string> StatusChanged;

        public DefaultIntentMatcher(IEmbeddingService embedding, IIntentRepository intentRepo)
        {
            _embedding = embedding;
            _intentRepo = intentRepo;
        }

        public List<MatchResult> Match(string inputText, int topK = 5)
        {
            if (string.IsNullOrWhiteSpace(inputText))
                return new List<MatchResult>();

            if (inputText.StartsWith("#"))
                return new List<MatchResult>();

            EnsureVectorsBuilt();

            var results = new List<MatchResult>();

            if (_vectorsBuilt && _vectors.Count > 0)
            {
                var inputVec = _embedding.GetEmbedding(inputText);
                if (inputVec != null)
                {
                    foreach (var intent in _intents)
                    {
                        if (!intent.IsActive) continue;
                        if (!_vectors.TryGetValue(intent.Id, out var intentVec)) continue;

                        double sim = _embedding.ComputeSimilarity(inputVec, intentVec);
                        results.Add(new MatchResult
                        {
                            Intent = intent,
                            Similarity = sim,
                            ConfidenceLevel = GetConfidenceLevel(sim),
                            IsFallback = false
                        });
                    }
                }
            }

            double threshold = 0.65;
            try { threshold = double.Parse(ConfigurationManager.AppSettings["MatchThreshold"] ?? "0.65"); } catch { }
            results = results.OrderByDescending(r => r.Similarity)
                .Where(r => r.Similarity >= threshold)
                .Take(topK)
                .ToList();

            if (results.Count == 0)
            {
                results = KeywordMatch(inputText, topK);
            }

            return results;
        }

        private List<MatchResult> KeywordMatch(string inputText, int topK)
        {
            var results = new List<MatchResult>();
            EnsureIntentsLoaded();

            foreach (var intent in _intents.Where(i => i.IsActive))
            {
                var keywords = (intent.Keywords ?? "").Split(new[] { ',', '，', ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                double score = 0;
                foreach (var kw in keywords)
                {
                    if (string.IsNullOrEmpty(kw)) continue;

                    if (inputText.Contains(kw))
                    {
                        score += 1.0 / keywords.Length;
                    }
                    else if (kw.Contains(inputText))
                    {
                        score += 0.8 / keywords.Length;
                    }
                    else
                    {
                        for (int len = Math.Min(4, kw.Length); len >= 2; len--)
                        {
                            bool found = false;
                            for (int i = 0; i <= kw.Length - len && !found; i++)
                            {
                                if (inputText.Contains(kw.Substring(i, len)))
                                {
                                    score += 0.5 / keywords.Length;
                                    found = true;
                                }
                            }
                            if (found) break;
                        }
                    }
                }

                if (score > 0)
                {
                    results.Add(new MatchResult
                    {
                        Intent = intent,
                        Similarity = Math.Min(score, 0.95),
                        ConfidenceLevel = "低（关键词）",
                        IsFallback = true
                    });
                }
            }

            return results.OrderByDescending(r => r.Similarity).Take(topK).ToList();
        }

        public void RebuildVectors()
        {
            _intents = _intentRepo.GetAll();
            _vectors.Clear();

            foreach (var intent in _intents.Where(i => i.IsActive))
            {
                var text = BuildSemanticText(intent);
                var vec = _embedding.GetEmbedding(text);
                if (vec != null)
                {
                    _vectors[intent.Id] = vec;
                    intent.Vector = vec;
                }
            }

            _vectorsBuilt = _vectors.Count > 0;
            StatusChanged?.Invoke(this, $"向量缓存已重建 ({_vectors.Count} 个意图)");
        }

        public void ReloadIntents()
        {
            _intents = null;
            _vectorsBuilt = false;
            EnsureIntentsLoaded();
        }

        private void EnsureIntentsLoaded()
        {
            if (_intents == null)
                _intents = _intentRepo.GetAll();
        }

        private void EnsureVectorsBuilt()
        {
            if (!_vectorsBuilt)
                RebuildVectors();
        }

        private static string BuildSemanticText(Intent intent)
        {
            var parts = new[] { intent.Name, intent.Description, intent.Keywords, intent.Category };
            return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        private static string GetConfidenceLevel(double similarity)
        {
            if (similarity >= 0.65) return "高";
            if (similarity >= 0.40) return "中";
            return "低";
        }
    }
}
