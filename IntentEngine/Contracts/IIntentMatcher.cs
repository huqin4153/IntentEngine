using System;
using System.Collections.Generic;

namespace IntentEngine.Contracts
{
    public interface IIntentMatcher
    {
        List<MatchResult> Match(string inputText, int topK = 5);

        void RebuildVectors();

        void ReloadIntents();

        event EventHandler<string> StatusChanged;
    }
}
