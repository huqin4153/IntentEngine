using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using IntentEngine.Models;

namespace IntentEngine.Controllers
{
    [RoutePrefix("api/datasource")]
    public class DataSourceController : ApiController
    {
        [HttpGet, Route("list")]
        public ApiResponse GetAll()
        {
            var list = new List<object>();

            foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
            {
                if (css.Name == "DefaultConfigDb" || css.Name == "LocalSqlServer" || css.Name == "LocalMySqlServer")
                    continue;
                if (!string.IsNullOrEmpty(css.ConnectionString))
                {
                    list.Add(new
                    {
                        Id = -1,
                        Name = css.Name,
                        ProviderType = string.IsNullOrEmpty(css.ProviderName) ? "SQLite" : css.ProviderName,
                        ConnectionString = MaskConnectionString(css.ConnectionString),
                        IsDefault = false,
                        Source = "web.config"
                    });
                }
            }

            try
            {
                var db = new Repositories.DatabaseInitializer();
                var dt = db.Query("SELECT * FROM DataSourceConfig ORDER BY Name");
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    var name = r["Name"]?.ToString();
                    if (list.Any(x => {
                        var n = x.GetType().GetProperty("Name")?.GetValue(x, null)?.ToString();
                        return n == name;
                    })) continue;

                    list.Add(new
                    {
                        Id = (int)(long)r["Id"],
                        Name = name,
                        ProviderType = r["ProviderType"]?.ToString() ?? "SQLite",
                        ConnectionString = MaskConnectionString(r["ConnectionString"]?.ToString()),
                        IsDefault = r["IsDefault"]?.ToString() == "1",
                        Source = "database"
                    });
                }
            }
            catch { }

            return ApiResponse.Ok(list);
        }

        [HttpPost, Route("save")]
        public ApiResponse Save(DataSourceConfig config)
        {
            if (config.Id < 0)
                return ApiResponse.Fail("web.config 中的连接串请直接编辑 Web.config 文件");

            var db = new Repositories.DatabaseInitializer();
            if (config.Id > 0)
            {
                db.Execute(@"UPDATE DataSourceConfig SET Name=@Name, ProviderType=@Provider,
                    ConnectionString=@Conn, IsDefault=@Def WHERE Id=@Id",
                    new[] {
                        new System.Data.SQLite.SQLiteParameter("@Id", config.Id),
                        new System.Data.SQLite.SQLiteParameter("@Name", config.Name),
                        new System.Data.SQLite.SQLiteParameter("@Provider", config.ProviderType),
                        new System.Data.SQLite.SQLiteParameter("@Conn", config.ConnectionString),
                        new System.Data.SQLite.SQLiteParameter("@Def", config.IsDefault ? 1 : 0)
                    });
            }
            else
            {
                config.Id = db.ExecuteScalar<int>(@"INSERT INTO DataSourceConfig (Name, ProviderType, ConnectionString, IsDefault)
                    VALUES (@Name, @Provider, @Conn, @Def); SELECT last_insert_rowid();",
                    new[] {
                        new System.Data.SQLite.SQLiteParameter("@Name", config.Name),
                        new System.Data.SQLite.SQLiteParameter("@Provider", config.ProviderType),
                        new System.Data.SQLite.SQLiteParameter("@Conn", config.ConnectionString),
                        new System.Data.SQLite.SQLiteParameter("@Def", config.IsDefault ? 1 : 0)
                    });
            }
            return ApiResponse.Ok(new { config.Id }, "保存成功");
        }

        [HttpPost, Route("delete")]
        public ApiResponse Delete(DeleteRequest request)
        {
            if (request.Id < 0)
                return ApiResponse.Fail("web.config 中的连接串请直接编辑 Web.config 文件");

            var db = new Repositories.DatabaseInitializer();
            db.Execute("DELETE FROM DataSourceConfig WHERE Id=@Id",
                new[] { new System.Data.SQLite.SQLiteParameter("@Id", request.Id) });
            return ApiResponse.Ok(null, "删除成功");
        }

        private static string MaskConnectionString(string connStr)
        {
            if (string.IsNullOrEmpty(connStr)) return "";
            return connStr
                .Replace("Password=", "Password=***")
                .Replace("pwd=", "pwd=***");
        }

        public class DeleteRequest { public int Id { get; set; } }
    }
}
