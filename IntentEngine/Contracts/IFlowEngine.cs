using System.Collections.Generic;

namespace IntentEngine.Contracts
{
    public interface IFlowEngine
    {
        List<Models.DisplayBlock> Execute(int functionId, Dictionary<string, string> userParams);

        List<Models.DisplayBlock> ExecuteWithLog(int functionId, Dictionary<string, string> userParams,
            string inputText, double similarity);
    }
}
