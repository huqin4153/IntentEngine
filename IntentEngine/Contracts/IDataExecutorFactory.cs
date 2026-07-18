using System;

namespace IntentEngine.Contracts
{
    public interface IDataExecutorFactory
    {
        void Register(string providerType, Func<string, IDataExecutor> factory);

        IDataExecutor GetExecutor(string dataSourceName);

        bool TestConnection(string dataSourceName);
    }
}
