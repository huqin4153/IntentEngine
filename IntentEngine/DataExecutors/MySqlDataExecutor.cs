using System;
using System.Collections.Generic;
using System.Data;
using IntentEngine.Contracts;

namespace IntentEngine.DataExecutors
{
    public class MySqlDataExecutor : IDataExecutor
    {
        public string ProviderName => "MySQL";
        public string ConnectionString { get; set; }

        public MySqlDataExecutor(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public DataTable ExecuteQuery(string sql, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException("请安装 MySql.Data NuGet 包并实现此方法");
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException("MySQL 执行器未实现");
        }

        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException("MySQL 执行器未实现");
        }

        public bool TestConnection() => false;

        public List<string> GetTableNames() => new List<string>();

        public void Dispose() { }
    }
}
