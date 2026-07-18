using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using IntentEngine.Contracts;

namespace IntentEngine.DataExecutors
{
    public class SqliteDataExecutor : IDataExecutor
    {
        public string ProviderName => "SQLite";
        public string ConnectionString { get; set; }

        public SqliteDataExecutor(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public DataTable ExecuteQuery(string sql, Dictionary<string, object> parameters)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    var da = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        public bool TestConnection()
        {
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    return conn.State == ConnectionState.Open;
                }
            }
            catch { return false; }
        }

        public List<string> GetTableNames()
        {
            var tables = new List<string>();
            try
            {
                var dt = ExecuteQuery("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name", null);
                return dt.AsEnumerable().Select(r => r[0]?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
            catch { return tables; }
        }

        public void Dispose() { }

        private void AddParameters(SQLiteCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;
            foreach (var kvp in parameters)
            {
                cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
            }
        }
    }
}
