using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using IntentEngine.Contracts;
using IntentEngine.Models;

namespace IntentEngine.Repositories
{
    public class FunctionParameterRepository : IFunctionParameterRepository
    {
        private DatabaseInitializer _db;
        private DatabaseInitializer Db => _db ?? (_db = new DatabaseInitializer());

        public List<FunctionParameter> GetByFunctionId(int functionId)
        {
            var dt = Db.Query("SELECT * FROM FunctionParameters WHERE FunctionId = @id ORDER BY SortOrder",
                new[] { new SQLiteParameter("@id", functionId) });
            return DataTableExtensions.ToList(dt, Map);
        }

        public FunctionParameter GetById(int id)
        {
            var dt = Db.Query("SELECT * FROM FunctionParameters WHERE Id = @id",
                new[] { new SQLiteParameter("@id", id) });
            if (dt.Rows.Count == 0) return null;
            return Map(dt.Rows[0]);
        }

        public int Insert(FunctionParameter param)
        {
            return Db.ExecuteScalar<int>(@"
                INSERT INTO FunctionParameters (FunctionId, Name, Label, ControlType, DataSource, IsRequired, DefaultValue, SortOrder)
                VALUES (@Fid, @Name, @Label, @Ctrl, @Src, @Req, @Def, @Sort);
                SELECT last_insert_rowid();",
                new[] {
                    new SQLiteParameter("@Fid", param.FunctionId),
                    new SQLiteParameter("@Name", param.Name),
                    new SQLiteParameter("@Label", (object)param.Label ?? ""),
                    new SQLiteParameter("@Ctrl", param.ControlType ?? "TextBox"),
                    new SQLiteParameter("@Src", (object)param.DataSource ?? ""),
                    new SQLiteParameter("@Req", param.IsRequired ? 1 : 0),
                    new SQLiteParameter("@Def", (object)param.DefaultValue ?? ""),
                    new SQLiteParameter("@Sort", param.SortOrder)
                });
        }

        public void Update(FunctionParameter param)
        {
            Db.Execute(@"UPDATE FunctionParameters SET Name=@Name, Label=@Label, ControlType=@Ctrl,
                    DataSource=@Src, IsRequired=@Req, DefaultValue=@Def, SortOrder=@Sort WHERE Id=@Id",
                new[] {
                    new SQLiteParameter("@Id", param.Id),
                    new SQLiteParameter("@Name", param.Name),
                    new SQLiteParameter("@Label", (object)param.Label ?? ""),
                    new SQLiteParameter("@Ctrl", param.ControlType ?? "TextBox"),
                    new SQLiteParameter("@Src", (object)param.DataSource ?? ""),
                    new SQLiteParameter("@Req", param.IsRequired ? 1 : 0),
                    new SQLiteParameter("@Def", (object)param.DefaultValue ?? ""),
                    new SQLiteParameter("@Sort", param.SortOrder)
                });
        }

        public void Delete(int id)
        {
            Db.Execute("DELETE FROM FunctionParameters WHERE Id=@Id", new[] { new SQLiteParameter("@Id", id) });
        }

        private FunctionParameter Map(DataRow row)
        {
            return new FunctionParameter
            {
                Id = (int)(long)row["Id"],
                FunctionId = (int)(long)row["FunctionId"],
                Name = row["Name"]?.ToString(),
                Label = row["Label"]?.ToString(),
                ControlType = row["ControlType"]?.ToString() ?? "TextBox",
                DataSource = row["DataSource"]?.ToString(),
                IsRequired = row["IsRequired"]?.ToString() == "1",
                DefaultValue = row["DefaultValue"]?.ToString(),
                SortOrder = (int)(long)row["SortOrder"]
            };
        }
    }
}
