using System;
using System.Collections.Generic;
using System.Configuration;
using IntentEngine.Contracts;
using System.Data.SQLite;

namespace IntentEngine.DataExecutors
{
    public class DataExecutorFactory : IDataExecutorFactory
    {
        private readonly Dictionary<string, Func<string, IDataExecutor>> _registry =
            new Dictionary<string, Func<string, IDataExecutor>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IDataExecutor> _cache =
            new Dictionary<string, IDataExecutor>(StringComparer.OrdinalIgnoreCase);

        public void Register(string providerType, Func<string, IDataExecutor> factory)
        {
            _registry[providerType] = factory;
        }

        public IDataExecutor GetExecutor(string dataSourceName)
        {
            if (_cache.TryGetValue(dataSourceName, out var cached))
                return cached;

            string connStr = null;
            string provider = null;

            try
            {
                var connSettings = ConfigurationManager.ConnectionStrings[dataSourceName];
                if (connSettings != null && !string.IsNullOrEmpty(connSettings.ConnectionString))
                {
                    connStr = connSettings.ConnectionString;
                    provider = string.IsNullOrEmpty(connSettings.ProviderName)
                        ? "SQLite" : connSettings.ProviderName;
                }
            }
            catch { }

            if (string.IsNullOrEmpty(connStr))
            {
                try
                {
                    var dbInit = new Repositories.DatabaseInitializer();
                    var dt = dbInit.Query(
                        "SELECT * FROM DataSourceConfig WHERE Name = @Name ORDER BY IsDefault DESC",
                        new[] { new SQLiteParameter("@Name", dataSourceName) });

                    if (dt.Rows.Count > 0)
                    {
                        connStr = dt.Rows[0]["ConnectionString"]?.ToString() ?? "";
                        provider = dt.Rows[0]["ProviderType"]?.ToString() ?? "SQLite";
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(connStr))
                return null;

            if (string.IsNullOrEmpty(provider))
                provider = "SQLite";

            if (_registry.TryGetValue(provider, out var factory))
            {
                var executor = factory(connStr);
                _cache[dataSourceName] = executor;
                return executor;
            }

            return null;
        }

        public bool TestConnection(string dataSourceName)
        {
            try
            {
                var executor = GetExecutor(dataSourceName);
                return executor?.TestConnection() ?? false;
            }
            catch { return false; }
        }
    }
}
