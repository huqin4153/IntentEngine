using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IntentEngine.Contracts;

namespace IntentEngine.Services
{
    public class CharEmbeddingService : IEmbeddingService
    {
        private const int DIMENSION = 768;
        private bool _isReady = true;

        public string ModelName => "char-embed-v1（纯 C# 语义嵌入）";
        public bool IsReady => _isReady;

        public event EventHandler<string> StatusChanged;

        public void Load(string modelPath, string vocabPath)
        {
            _isReady = true;
            StatusChanged?.Invoke(this, "字符语义引擎已就绪");
        }

        public void Dispose()
        {
        }

        public float[] GetEmbedding(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new float[DIMENSION];

            var vector = new double[DIMENSION];

            var ngrams = ExtractNgrams(text, 1, 3);

            foreach (var ngram in ngrams)
            {
                int hash1 = (int)(((uint)ngram.GetHashCode()) % DIMENSION);
                int hash2 = (int)(((uint)ngram.GetHashCode() ^ 0xDEADBEEF) % DIMENSION);

                int sign = (hash2 % 2 == 0) ? 1 : -1;

                vector[Math.Abs(hash1)] += sign * 1.0;
            }

            double norm = Math.Sqrt(vector.Sum(v => v * v));
            if (norm > 1e-10)
            {
                for (int i = 0; i < DIMENSION; i++)
                    vector[i] /= norm;
            }

            return vector.Select(v => (float)v).ToArray();
        }

        public float[][] GetEmbeddings(string[] texts)
        {
            return texts.Select(GetEmbedding).ToArray();
        }

        public double ComputeSimilarity(float[] vec1, float[] vec2)
        {
            if (vec1 == null || vec2 == null || vec1.Length != vec2.Length)
                return 0;

            double dot = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < vec1.Length; i++)
            {
                dot += vec1[i] * vec2[i];
                norm1 += vec1[i] * vec1[i];
                norm2 += vec2[i] * vec2[i];
            }

            double denom = Math.Sqrt(norm1) * Math.Sqrt(norm2);
            return denom > 1e-10 ? dot / denom : 0;
        }

        private static List<string> ExtractNgrams(string text, int minN, int maxN)
        {
            var result = new List<string>();

            text = text.ToLowerInvariant().Replace(" ", "");

            if (string.IsNullOrEmpty(text)) return result;

            var chars = text.ToCharArray();

            for (int n = minN; n <= maxN; n++)
            {
                for (int i = 0; i <= chars.Length - n; i++)
                {
                    var sb = new StringBuilder(n);
                    for (int j = 0; j < n; j++)
                        sb.Append(chars[i + j]);

                    result.Add(sb.ToString());
                }
            }

            return result;
        }
    }
}
