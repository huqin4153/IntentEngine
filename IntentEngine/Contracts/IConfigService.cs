using System.Collections.Generic;

namespace IntentEngine.Contracts
{
    public interface IConfigService
    {
        List<Models.Intent> GetIntentTree();

        void RebuildVectors();

        List<string> ValidateConfiguration();

        string ExportConfiguration();

        void ImportConfiguration(string json);
    }
}
