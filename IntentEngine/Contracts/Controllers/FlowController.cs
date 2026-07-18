using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using IntentEngine.Models;

namespace IntentEngine.Controllers
{
    [RoutePrefix("api/flow")]
    public class FlowController : ApiController
    {
        [HttpPost, Route("functions")]
        public ApiResponse GetFunctions([FromBody] FunctionsRequest request)
        {
            if (request == null || request.IntentId <= 0)
                return ApiResponse.Fail("参数错误");

            var funcs = IocConfig.Container.GetFunctionRepo().GetByIntentId(request.IntentId);
            return ApiResponse.Ok(funcs.Select(f => new
            {
                f.Id, f.Name, f.Description, f.SortOrder
            }));
        }

        [HttpPost, Route("parameters")]
        public ApiResponse GetParameters([FromBody] ParametersRequest request)
        {
            if (request == null || request.FunctionId <= 0)
                return ApiResponse.Fail("参数错误");

            var @params = IocConfig.Container.GetParamRepo().GetByFunctionId(request.FunctionId);
            return ApiResponse.Ok(@params.Select(p => new
            {
                p.Id,
                p.Name,
                p.Label,
                p.ControlType,
                DataSource = !string.IsNullOrEmpty(p.DataSource)
                    ? Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(p.DataSource)
                    : null,
                p.IsRequired,
                p.DefaultValue,
                p.SortOrder
            }));
        }

        [HttpPost, Route("execute")]
        public ApiResponse Execute(ExecuteRequest request)
        {
            if (request == null || request.FunctionId <= 0)
                return ApiResponse.Fail("参数错误");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var engine = IocConfig.Container.GetFlowEngine();
            var blocks = engine.Execute(request.FunctionId, request.Params ?? new Dictionary<string, string>());
            sw.Stop();

            try
            {
                var dbInit = new Repositories.DatabaseInitializer();
                dbInit.Execute(@"INSERT INTO QueryLog (InputText, Parameters, ElapsedMs) VALUES (@Input, @Params, @Ms)",
                    new[] {
                        new System.Data.SQLite.SQLiteParameter("@Input", ""),
                        new System.Data.SQLite.SQLiteParameter("@Params", Newtonsoft.Json.JsonConvert.SerializeObject(request.Params)),
                        new System.Data.SQLite.SQLiteParameter("@Ms", sw.ElapsedMilliseconds)
                    });
            }
            catch { }

            var data = blocks.Select(b => new
            {
                b.Type,
                b.Title,
                tableData = b.Type == "table" && b.TableData != null
                    ? SerializeTable(b.TableData)
                    : null,
                textContent = b.Type != "table" ? b.TextContent : null,
                b.ConfigJson
            });

            var stepRepo = IocConfig.Container.GetFlowStepRepo();
            var steps = stepRepo.GetByFunctionId(request.FunctionId);
            var firstStep = steps.FirstOrDefault();
            string functionName = request.FunctionId.ToString();
            if (firstStep != null)
            {
                var func = IocConfig.Container.GetFunctionRepo().GetById(request.FunctionId);
                if (func != null) functionName = func.Name;
            }

            var execFunc = IocConfig.Container.GetFunctionRepo().GetById(request.FunctionId);
            var dbgDataSource = execFunc?.DataSource ?? "Config";

            string dbgProvider = "none";
            string dbgConnStr = "";
            try
            {
                var factory = IocConfig.Container.GetExecutorFactory();
                var exec = factory.GetExecutor(dbgDataSource);
                if (exec != null)
                {
                    dbgProvider = exec.ProviderName;
                    dbgConnStr = exec.ConnectionString?.Replace("Password=", "PWD=***").Replace("pwd=", "PWD=***") ?? "";
                }
            }
            catch { }

            return ApiResponse.Ok(new
            {
                functionName,
                dataSource = dbgDataSource,
                dbgProvider,
                blocks = data,
                elapsedMs = sw.ElapsedMilliseconds
            });
        }

        private static object SerializeTable(System.Data.DataTable dt)
        {
            if (dt == null) return null;
            var cols = dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName.ToLowerInvariant()).ToArray();
            var rows = dt.AsEnumerable().Select(r =>
            {
                var dict = new Dictionary<string, object>();
                foreach (var c in dt.Columns.Cast<System.Data.DataColumn>())
                    dict[c.ColumnName.ToLowerInvariant()] = r[c];
                return dict;
            }).ToList();
            return new { columns = cols, rows };
        }

        public class FunctionsRequest
        {
            public int IntentId { get; set; }
        }

        public class ParametersRequest
        {
            public int FunctionId { get; set; }
        }

        public class ExecuteRequest
        {
            public int FunctionId { get; set; }
            public Dictionary<string, string> Params { get; set; }
        }
    }
}
