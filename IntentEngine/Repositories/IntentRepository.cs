using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using IntentEngine.Contracts;
using IntentEngine.Models;

namespace IntentEngine.Repositories
{
    public class IntentRepository : IIntentRepository
    {
        private DatabaseInitializer _db;
        private DatabaseInitializer Db => _db ?? (_db = new DatabaseInitializer());

        public List<Intent> GetAll()
        {
            var dt = Db.Query("SELECT * FROM Intents ORDER BY Category, Name");
            return DataTableExtensions.ToList(dt, Map);
        }

        public List<Intent> GetActive()
        {
            var dt = Db.Query("SELECT * FROM Intents WHERE IsActive = 1 ORDER BY Category, Name");
            return DataTableExtensions.ToList(dt, Map);
        }

        public Intent GetById(int id)
        {
            var dt = Db.Query("SELECT * FROM Intents WHERE Id = @id",
                new[] { new SQLiteParameter("@id", id) });
            if (dt.Rows.Count == 0) return null;
            return Map(dt.Rows[0]);
        }

        public int Insert(Intent intent)
        {
            int id = Db.ExecuteScalar<int>(@"
                INSERT INTO Intents (Name, Description, Keywords, Category, SemanticText, IsActive)
                VALUES (@Name, @Desc, @Keywords, @Category, @Semantic, @Active);
                SELECT last_insert_rowid();",
                new[] {
                    new SQLiteParameter("@Name", intent.Name),
                    new SQLiteParameter("@Desc", (object)intent.Description ?? ""),
                    new SQLiteParameter("@Keywords", (object)intent.Keywords ?? ""),
                    new SQLiteParameter("@Category", (object)intent.Category ?? ""),
                    new SQLiteParameter("@Semantic", (object)intent.SemanticText ?? ""),
                    new SQLiteParameter("@Active", intent.IsActive ? 1 : 0)
                });
            return id;
        }

        public void Update(Intent intent)
        {
            Db.Execute(@"UPDATE Intents SET Name=@Name, Description=@Desc, Keywords=@Keywords,
                         Category=@Category, SemanticText=@Semantic, IsActive=@Active WHERE Id=@Id",
                new[] {
                    new SQLiteParameter("@Id", intent.Id),
                    new SQLiteParameter("@Name", intent.Name),
                    new SQLiteParameter("@Desc", (object)intent.Description ?? ""),
                    new SQLiteParameter("@Keywords", (object)intent.Keywords ?? ""),
                    new SQLiteParameter("@Category", (object)intent.Category ?? ""),
                    new SQLiteParameter("@Semantic", (object)intent.SemanticText ?? ""),
                    new SQLiteParameter("@Active", intent.IsActive ? 1 : 0)
                });
        }

        public void Delete(int id)
        {
            Db.Execute("DELETE FROM Intents WHERE Id=@Id", new[] { new SQLiteParameter("@Id", id) });
        }

        private Intent Map(System.Data.DataRow row)
        {
            return new Intent
            {
                Id = (int)(long)row["Id"],
                Name = row["Name"]?.ToString(),
                Description = row["Description"]?.ToString(),
                Keywords = row["Keywords"]?.ToString(),
                Category = row["Category"]?.ToString(),
                SemanticText = row["SemanticText"]?.ToString(),
                IsActive = row["IsActive"]?.ToString() == "1",
                CreatedAt = System.DateTime.TryParse(row["CreatedAt"]?.ToString(), out var d) ? d : System.DateTime.Now
            };
        }
    }
}
