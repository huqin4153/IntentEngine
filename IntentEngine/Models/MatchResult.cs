using IntentEngine.Models;

namespace IntentEngine.Contracts
{
    public class MatchResult
    {
        public Intent Intent { get; set; }
        public double Similarity { get; set; }
        public string ConfidenceLevel { get; set; }
        public bool IsFallback { get; set; }
    }
}
