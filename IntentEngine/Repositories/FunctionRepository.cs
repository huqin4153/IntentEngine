using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using IntentEngine.Contracts;
using IntentEngine.Models;

namespace IntentEngine.Repositories
{
    public class FunctionRepository : IFunctionRepository
    {
        private DatabaseInitializer _db;
        private DatabaseInitializer Db => _db ?? (_db = new DatabaseInitializer());

        public List<Function> GetByIntentId(int intentId)
        {
            var dt = Db.Query("SELECT * FROM Functions WHERE IntentId = @id ORDER BY SortOrder",
                new[] { new SQLiteParameter("@id", intentId) });
            return dt.AsEnumerable().Select(Map).ToList();
        }

        public Function GetById(int id)
        {
            var dt = Db.Query("SELECT * FROM Functions WHERE Id = @id",
                new[] { new SQLiteParameter("@id", id) });
            if (dt.Rows.Count == 0) return null;
            return Map(dt.Rows[0]);
        }

        public int Insert(Function function)
        {
            return Db.ExecuteScalar<int>(@"
                INSERT INTO Functions (IntentId, Name, Description, SortOrder, DataSource)
                VALUES (@IntentId, @Name, @Desc, @Sort, @DS);
                SELECT last_insert_rowid();",
                new[] {
                    new SQLiteParameter("@IntentId", function.IntentId),
                    new SQLiteParameter("@Name", function.Name),
                    new SQLiteParameter("@Desc", (object)function.Description ?? ""),
                    new SQLiteParameter("@Sort", function.SortOrder),
                    new SQLiteParameter("@DS", (object)function.DataSource ?? "Config")
                });
        }

        public void Update(Function function)
        {
            Db.Execute(@"UPDATE Functions SET Name=@Name, Description=@Desc, SortOrder=@Sort, DataSource=@DS WHERE Id=@Id",
                new[] {
                    new SQLiteParameter("@Id", function.Id),
                    new SQLiteParameter("@Name", function.Name),
                    new SQLiteParameter("@Desc", (object)function.Description ?? ""),
                    new SQLiteParameter("@Sort", function.SortOrder),
                    new SQLiteParameter("@DS", (object)function.DataSource ?? "Config")
                });
        }

        public void Delete(int id)
        {
            Db.Execute("DELETE FROM Functions WHERE Id=@Id", new[] { new SQLiteParameter("@Id", id) });
        }

        public void Reorder(int intentId, List<int> functionIds)
        {
            for (int i = 0; i < functionIds.Count; i++)
            {
                Db.Execute("UPDATE Functions SET SortOrder=@Sort WHERE Id=@Id AND IntentId=@IntentId",
                    new[] {
                        new SQLiteParameter("@Id", functionIds[i]),
                        new SQLiteParameter("@IntentId", intentId),
                        new SQLiteParameter("@Sort", i)
                    });
            }
        }

        private Function Map(DataRow row)
        {
            return new Function
            {
                Id = (int)(long)row["Id"],
                IntentId = (int)(long)row["IntentId"],
                Name = row["Name"]?.ToString(),
                Description = row["Description"]?.ToString(),
                SortOrder = (int)(long)row["SortOrder"],
                DataSource = row["DataSource"]?.ToString() ?? "Config"
            };
        }
    }
}
