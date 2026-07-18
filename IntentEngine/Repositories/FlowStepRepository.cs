using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using IntentEngine.Contracts;
using IntentEngine.Models;

namespace IntentEngine.Repositories
{
    public class FlowStepRepository : IFlowStepRepository
    {
        private DatabaseInitializer _db;
        private DatabaseInitializer Db => _db ?? (_db = new DatabaseInitializer());

        public List<FlowStep> GetByFunctionId(int functionId)
        {
            var dt = Db.Query("SELECT * FROM FlowSteps WHERE FunctionId = @id ORDER BY SortOrder",
                new[] { new SQLiteParameter("@id", functionId) });
            return DataTableExtensions.ToList(dt, Map);
        }

        public FlowStep GetById(int id)
        {
            var dt = Db.Query("SELECT * FROM FlowSteps WHERE Id = @id",
                new[] { new SQLiteParameter("@id", id) });
            if (dt.Rows.Count == 0) return null;
            return Map(dt.Rows[0]);
        }

        public int Insert(FlowStep step)
        {
            return Db.ExecuteScalar<int>(@"
                INSERT INTO FlowSteps (FunctionId, SortOrder, StepType, Label, SqlText, ResultVar, DataSource,
                    ExpectOperator, ExpectValue, ExpectOnFail, ExpectTarget, ExpectMessage,
                    DisplayTitle, DisplaySource, DisplayConfig, IsEnd)
                VALUES (@Fid, @Sort, @Type, @Label, @Sql, @Var, @DSrc,
                    @ExpOpr, @ExpVal, @ExpFail, @ExpTgt, @ExpMsg,
                    @DispTitle, @DispSrc, @DispCfg, @End);
                SELECT last_insert_rowid();",
                new[] {
                    new SQLiteParameter("@Fid", step.FunctionId),
                    new SQLiteParameter("@Sort", step.SortOrder),
                    new SQLiteParameter("@Type", step.StepType),
                    new SQLiteParameter("@Label", (object)step.Label ?? ""),
                    new SQLiteParameter("@Sql", (object)step.SqlText ?? ""),
                    new SQLiteParameter("@Var", (object)step.ResultVar ?? ""),
                    new SQLiteParameter("@DSrc", (object)step.DataSource ?? "Config"),
                    new SQLiteParameter("@ExpOpr", (object)step.ExpectOperator ?? ""),
                    new SQLiteParameter("@ExpVal", (object)step.ExpectValue ?? ""),
                    new SQLiteParameter("@ExpFail", (object)step.ExpectOnFail ?? ""),
                    new SQLiteParameter("@ExpTgt", (object)step.ExpectTarget ?? System.DBNull.Value),
                    new SQLiteParameter("@ExpMsg", (object)step.ExpectMessage ?? ""),
                    new SQLiteParameter("@DispTitle", (object)step.DisplayTitle ?? ""),
                    new SQLiteParameter("@DispSrc", (object)step.DisplaySource ?? ""),
                    new SQLiteParameter("@DispCfg", (object)step.DisplayConfig ?? "{}"),
                    new SQLiteParameter("@End", step.IsEnd ? 1 : 0)
                });
        }

        public void Update(FlowStep step)
        {
            Db.Execute(@"UPDATE FlowSteps SET SortOrder=@Sort, StepType=@Type, Label=@Label,
                    SqlText=@Sql, ResultVar=@Var, DataSource=@DSrc,
                    ExpectOperator=@ExpOpr, ExpectValue=@ExpVal, ExpectOnFail=@ExpFail,
                    ExpectTarget=@ExpTgt, ExpectMessage=@ExpMsg,
                    DisplayTitle=@DispTitle, DisplaySource=@DispSrc, DisplayConfig=@DispCfg,
                    IsEnd=@End WHERE Id=@Id",
                new[] {
                    new SQLiteParameter("@Id", step.Id),
                    new SQLiteParameter("@Sort", step.SortOrder),
                    new SQLiteParameter("@Type", step.StepType),
                    new SQLiteParameter("@Label", (object)step.Label ?? ""),
                    new SQLiteParameter("@Sql", (object)step.SqlText ?? ""),
                    new SQLiteParameter("@Var", (object)step.ResultVar ?? ""),
                    new SQLiteParameter("@DSrc", (object)step.DataSource ?? "Config"),
                    new SQLiteParameter("@ExpOpr", (object)step.ExpectOperator ?? ""),
                    new SQLiteParameter("@ExpVal", (object)step.ExpectValue ?? ""),
                    new SQLiteParameter("@ExpFail", (object)step.ExpectOnFail ?? ""),
                    new SQLiteParameter("@ExpTgt", (object)step.ExpectTarget ?? System.DBNull.Value),
                    new SQLiteParameter("@ExpMsg", (object)step.ExpectMessage ?? ""),
                    new SQLiteParameter("@DispTitle", (object)step.DisplayTitle ?? ""),
                    new SQLiteParameter("@DispSrc", (object)step.DisplaySource ?? ""),
                    new SQLiteParameter("@DispCfg", (object)step.DisplayConfig ?? "{}"),
                    new SQLiteParameter("@End", step.IsEnd ? 1 : 0)
                });
        }

        public void Delete(int id)
        {
            Db.Execute("DELETE FROM FlowSteps WHERE Id=@Id", new[] { new SQLiteParameter("@Id", id) });
        }

        public void Reorder(int functionId, List<int> stepIds)
        {
            for (int i = 0; i < stepIds.Count; i++)
            {
                Db.Execute("UPDATE FlowSteps SET SortOrder=@Sort WHERE Id=@Id AND FunctionId=@Fid",
                    new[] {
                        new SQLiteParameter("@Id", stepIds[i]),
                        new SQLiteParameter("@Fid", functionId),
                        new SQLiteParameter("@Sort", i)
                    });
            }
        }

        private FlowStep Map(DataRow row)
        {
            return new FlowStep
            {
                Id = (int)(long)row["Id"],
                FunctionId = (int)(long)row["FunctionId"],
                SortOrder = (int)(long)row["SortOrder"],
                StepType = row["StepType"]?.ToString(),
                Label = row["Label"]?.ToString(),
                SqlText = row["SqlText"]?.ToString(),
                ResultVar = row["ResultVar"]?.ToString(),
                DataSource = row["DataSource"]?.ToString() ?? "Config",
                ExpectOperator = row["ExpectOperator"]?.ToString(),
                ExpectValue = row["ExpectValue"]?.ToString(),
                ExpectOnFail = row["ExpectOnFail"]?.ToString(),
                ExpectTarget = row["ExpectTarget"] != System.DBNull.Value ? (int?)(long)row["ExpectTarget"] : null,
                ExpectMessage = row["ExpectMessage"]?.ToString(),
                DisplayTitle = row["DisplayTitle"]?.ToString(),
                DisplaySource = row["DisplaySource"]?.ToString(),
                DisplayConfig = row["DisplayConfig"]?.ToString() ?? "{}",
                IsEnd = row["IsEnd"]?.ToString() == "1"
            };
        }
    }
}
