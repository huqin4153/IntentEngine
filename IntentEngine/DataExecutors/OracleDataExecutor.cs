using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using IntentEngine.Contracts;
using Oracle.DataAccess.Client;

namespace IntentEngine.DataExecutors
{
    public class OracleDataExecutor : IDataExecutor
    {
        public string ProviderName => "Oracle";
        public string ConnectionString { get; set; }

        public OracleDataExecutor(string connectionString) { ConnectionString = connectionString; }

        public DataTable ExecuteQuery(string sql, Dictionary<string, object> parameters)
        {
            List<OracleParameter> oraParams;
            string execSql = ConvertToOracle(sql, parameters, out oraParams);

            using (var orcl = new OracleConnection(ConnectionString))
            {
                orcl.Open();
                using (var cmd = orcl.CreateCommand())
                {
                    cmd.CommandText = execSql;
                    cmd.BindByName = true;
                    foreach (var p in oraParams) cmd.Parameters.Add(p);
                    using (var da = new OracleDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            List<OracleParameter> oraParams;
            string execSql = ConvertToOracle(sql, parameters, out oraParams);
            using (var orcl = new OracleConnection(ConnectionString))
            {
                orcl.Open();
                using (var cmd = orcl.CreateCommand())
                {
                    cmd.CommandText = execSql;
                    cmd.BindByName = true;
                    foreach (var p in oraParams) cmd.Parameters.Add(p);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
        {
            List<OracleParameter> oraParams;
            string execSql = ConvertToOracle(sql, parameters, out oraParams);
            using (var orcl = new OracleConnection(ConnectionString))
            {
                orcl.Open();
                using (var cmd = orcl.CreateCommand())
                {
                    cmd.CommandText = execSql;
                    cmd.BindByName = true;
                    foreach (var p in oraParams) cmd.Parameters.Add(p);
                    return cmd.ExecuteScalar();
                }
            }
        }

        public bool TestConnection()
        {
            try { using (var orcl = new OracleConnection(ConnectionString)) { orcl.Open(); return orcl.State == ConnectionState.Open; } }
            catch { return false; }
        }

        public List<string> GetTableNames()
        {
            var dt = ExecuteQuery("SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME", null);
            return dt.AsEnumerable().Select(r => r[0]?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        public void Dispose() { }

        private string ConvertToOracle(string sql, Dictionary<string, object> inputParams,
            out List<OracleParameter> outputParams)
        {
            outputParams = new List<OracleParameter>();
            if (inputParams == null || inputParams.Count == 0) return sql;

            var sorted = inputParams.Keys.OrderByDescending(k => k.Length).ToList();
            string result = sql;
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in sorted)
            {
                string name = key.TrimStart('@', ':');
                result = result.Replace("@" + name, ":" + name);
                if (!added.Contains(name))
                {
                    added.Add(name);
                    outputParams.Add(new OracleParameter(":" + name, inputParams[key] ?? DBNull.Value));
                }
            }
            return result;
        }
    }
}
