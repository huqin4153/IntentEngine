using System;

namespace IntentEngine.Contracts
{
    public interface IEmbeddingService : IDisposable
    {
        string ModelName { get; }

        bool IsReady { get; }

        void Load(string modelPath, string vocabPath);

        float[] GetEmbedding(string text);

        float[][] GetEmbeddings(string[] texts);

        double ComputeSimilarity(float[] vec1, float[] vec2);

        event EventHandler<string> StatusChanged;
    }
}
