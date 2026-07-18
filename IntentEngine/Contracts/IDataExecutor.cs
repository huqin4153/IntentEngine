using System.Data;

namespace IntentEngine.Contracts
{
    public interface IDataExecutor : System.IDisposable
    {
        string ProviderName { get; }
        string ConnectionString { get; set; }

        DataTable ExecuteQuery(string sql, System.Collections.Generic.Dictionary<string, object> parameters);
        int ExecuteNonQuery(string sql, System.Collections.Generic.Dictionary<string, object> parameters);
        object ExecuteScalar(string sql, System.Collections.Generic.Dictionary<string, object> parameters);
        bool TestConnection();
        System.Collections.Generic.List<string> GetTableNames();
    }
}
