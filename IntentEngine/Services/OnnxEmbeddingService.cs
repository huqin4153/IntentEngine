using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IntentEngine.Contracts;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace IntentEngine.Services
{
    public class OnnxEmbeddingService : IEmbeddingService
    {
        private bool _isReady = false;
        private InferenceSession _session;
        private Tokenizer _tokenizer;
        private bool _disposed = false;
        public string LastError { get; private set; }

        public string ModelName => "bge-small-zh-v1.5 (ONNX)";
        public bool IsReady => _isReady;
        public event EventHandler<string> StatusChanged;

        private const int SEQUENCE_LENGTH = 128;
        private const int HIDDEN_SIZE = 384;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        public void Load(string modelPath, string vocabPath)
        {
            LastError = null;
            try
            {
                if (!File.Exists(modelPath))
                    throw new FileNotFoundException("模型文件不存在: " + modelPath);

                _tokenizer = new Tokenizer(SEQUENCE_LENGTH);
                if (!string.IsNullOrEmpty(vocabPath) && File.Exists(vocabPath))
                    _tokenizer.LoadVocab(vocabPath);

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                bool loaded = false;
                string loadError = "";
                foreach (var dir in new[] { baseDir, Path.Combine(baseDir, "bin") })
                {
                    string p = Path.Combine(dir, "onnxruntime.dll");
                    if (File.Exists(p))
                    {
                        IntPtr h = LoadLibrary(p);
                        if (h != IntPtr.Zero) { loaded = true; break; }
                        else {
                            int err = Marshal.GetLastWin32Error();
                            loadError = $"文件存在但LoadLibrary失败 路径={p} 错误码={err}";
                            LastError = loadError;
                        }
                    }
                    else
                    {
                        loadError = (loadError == "" ? "" : loadError + "; ") + $"文件不存在 路径={p}";
                    }
                }

                if (!loaded)
                    throw new Exception("onnxruntime.dll 无法加载。" + loadError + "。搜索目录: " + baseDir);

                _session = new InferenceSession(modelPath);
                _isReady = true;
                StatusChanged?.Invoke(this, "ONNX 模型已加载");
            }
            catch (Exception ex)
            {
                _isReady = false;
                LastError = ex.ToString();
                _session?.Dispose();
                _session = null;
            }
        }

        public float[] GetEmbedding(string text)
        {
            if (!_isReady || _session == null || _tokenizer == null) return null;
            try
            {
                var encoded = _tokenizer.Encode(text ?? "");
                int[] shape = { 1, SEQUENCE_LENGTH };
                var inputIds = new DenseTensor<long>(encoded.InputIds, shape);
                var mask = new DenseTensor<long>(encoded.AttentionMask, shape);
                var tokenIds = new DenseTensor<long>(encoded.TokenTypeIds, shape);
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                    NamedOnnxValue.CreateFromTensor("attention_mask", mask),
                    NamedOnnxValue.CreateFromTensor("token_type_ids", tokenIds)
                };
                using (var results = _session.Run(inputs))
                {
                    var output = results.FirstOrDefault();
                    if (output == null) return null;
                    var tensor = output.AsTensor<float>();
                    if (tensor == null) return null;
                    var allData = tensor.ToArray();
                    return allData.Length == 0 ? null : MeanPool(allData);
                }
            }
            catch (Exception ex) { LastError = ex.ToString(); return null; }
        }

        private float[] MeanPool(float[] allData)
        {
            int totalCount = allData.Length;
            int hiddenSize = HIDDEN_SIZE;
            int tokenCount = totalCount / hiddenSize;
            float[] pooled = new float[hiddenSize];
            int validTokens = 0;
            for (int t = 0; t < tokenCount && t < SEQUENCE_LENGTH; t++)
            {
                validTokens++;
                for (int d = 0; d < hiddenSize; d++) pooled[d] += allData[t * hiddenSize + d];
            }
            if (validTokens > 0)
                for (int d = 0; d < hiddenSize; d++) pooled[d] /= validTokens;
            float norm = 0;
            for (int d = 0; d < hiddenSize; d++) norm += pooled[d] * pooled[d];
            norm = (float)Math.Sqrt(norm);
            if (norm > 1e-10f) for (int d = 0; d < hiddenSize; d++) pooled[d] /= norm;
            return pooled;
        }

        public float[][] GetEmbeddings(string[] texts) => texts.Select(GetEmbedding).ToArray();
        public double ComputeSimilarity(float[] v1, float[] v2)
        {
            if (v1 == null || v2 == null || v1.Length != v2.Length) return 0;
            double dot = 0, n1 = 0, n2 = 0;
            for (int i = 0; i < v1.Length; i++) { dot += v1[i] * v2[i]; n1 += v1[i] * v1[i]; n2 += v2[i] * v2[i]; }
            double d = Math.Sqrt(n1) * Math.Sqrt(n2);
            return d > 1e-10 ? dot / d : 0;
        }

        ~OnnxEmbeddingService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _tokenizer = null;
            }

            if (_session != null)
            {
                try { _session.Dispose(); }
                catch { }
                _session = null;
            }
            _isReady = false;
        }
    }
}
