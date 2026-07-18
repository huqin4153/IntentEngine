using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using IntentEngine.Contracts;
using IntentEngine.Models;

namespace IntentEngine.Services
{
    public class DefaultFlowEngine : IFlowEngine
    {
        private readonly IFlowStepRepository _stepRepo;
        private readonly IFunctionParameterRepository _paramRepo;
        private readonly IDataExecutorFactory _executorFactory;
        private readonly IFunctionRepository _funcRepo;

        public DefaultFlowEngine(IFlowStepRepository stepRepo, IFunctionParameterRepository paramRepo,
            IDataExecutorFactory executorFactory, IFunctionRepository funcRepo)
        {
            _stepRepo = stepRepo;
            _paramRepo = paramRepo;
            _executorFactory = executorFactory;
            _funcRepo = funcRepo;
        }

        public List<DisplayBlock> Execute(int functionId, Dictionary<string, string> userParams)
        {
            var func = _funcRepo.GetById(functionId);
            string funcDataSource = func?.DataSource;
            if (string.IsNullOrEmpty(funcDataSource)) funcDataSource = "Config";

            var steps = _stepRepo.GetByFunctionId(functionId);
            var ctx = new Dictionary<string, string>(userParams ?? new Dictionary<string, string>());
            var vars = new Dictionary<string, ResultSet>();
            var displays = new List<DisplayBlock>();
            var stepList = steps.OrderBy(s => s.SortOrder).ToList();
            int idx = 0;

            while (idx >= 0 && idx < stepList.Count)
            {
                var step = stepList[idx];
                idx++;

                switch (step.StepType.ToLower())
                {
                    case "sql":
                        ExecuteSqlStep(step, ctx, vars, stepList, ref idx, displays, funcDataSource);
                        break;
                    case "show_table":
                        if (step.DisplaySource != null && vars.TryGetValue(step.DisplaySource, out var tableResult))
                            displays.Add(new DisplayBlock { Type = "table", Title = step.DisplayTitle ?? "查询结果", TableData = tableResult.TableData, ConfigJson = step.DisplayConfig });
                        break;
                    case "show_text":
                        if (step.DisplaySource != null && vars.TryGetValue(step.DisplaySource, out var textResult))
                        {
                            var summary = BuildTextSummary(step, textResult, ctx);
                            displays.Add(new DisplayBlock { Type = "text", Title = step.DisplayTitle ?? "信息", TextContent = summary, ConfigJson = step.DisplayConfig });
                        }
                        break;
                    case "show_error":
                        string errMsg = step.ExpectMessage;
                        if (string.IsNullOrEmpty(errMsg) && !string.IsNullOrEmpty(step.DisplayConfig))
                        {
                            try
                            {
                                var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(step.DisplayConfig);
                                if (cfg.TryGetValue("template", out var tpl)) errMsg = tpl;
                            }
                            catch { }
                        }
                        if (string.IsNullOrEmpty(errMsg)) errMsg = step.DisplayTitle ?? "未知错误";
                        var msg = TemplateEngine.ReplaceText(errMsg, ctx);
                        displays.Add(new DisplayBlock { Type = "error", Title = step.DisplayTitle ?? "提示", TextContent = msg, ConfigJson = step.DisplayConfig });
                        idx = -1;
                        break;
                    case "end":
                        idx = -1;
                        break;
                }
            }
            return displays;
        }

        public List<DisplayBlock> ExecuteWithLog(int functionId, Dictionary<string, string> userParams,
            string inputText, double similarity)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = Execute(functionId, userParams);
            sw.Stop();
            try
            {
                var dbInit = new Repositories.DatabaseInitializer();
                dbInit.Execute(@"INSERT INTO QueryLog (InputText, Parameters, Similarity, ElapsedMs) VALUES (@Input, @Params, @Sim, @Ms)",
                    new[] {
                        new System.Data.SQLite.SQLiteParameter("@Input", inputText ?? ""),
                        new System.Data.SQLite.SQLiteParameter("@Params", Newtonsoft.Json.JsonConvert.SerializeObject(userParams)),
                        new System.Data.SQLite.SQLiteParameter("@Sim", similarity),
                        new System.Data.SQLite.SQLiteParameter("@Ms", sw.ElapsedMilliseconds)
                    });
            }
            catch { }
            return result;
        }

        private void ExecuteSqlStep(FlowStep step, Dictionary<string, string> ctx,
            Dictionary<string, ResultSet> vars, List<FlowStep> stepList,
            ref int idx, List<DisplayBlock> displays, string funcDataSource)
        {
            Dictionary<string, string> parameters;
            string sql = TemplateEngine.ReplaceSql(step.SqlText, ctx, out parameters, "@");

            var result = ExecuteSql(sql, parameters, funcDataSource);
            result.SqlIndex = step.SortOrder;

            bool debugSql = string.Equals(
                System.Configuration.ConfigurationManager.AppSettings["DebugSql"], "true",
                StringComparison.OrdinalIgnoreCase);
            if (debugSql)
            {
                string dbgParamStr = parameters.Count > 0
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(parameters)
                    : "(无参数)";
                string displaySql = sql;
                bool isOracle = (funcDataSource == "Oracle" || funcDataSource == "BusinessDB");
                if (isOracle && parameters.Count > 0)
                {
                    displaySql = sql;
                    var sorted = parameters.Keys.OrderByDescending(k => k.Length).ToList();
                    foreach (var k in sorted)
                        displaySql = displaySql.Replace("@" + k, ":" + k);
                }
                displays.Add(new DisplayBlock
                {
                    Type = "text",
                    Title = "🔍 SQL 诊断 (" + funcDataSource + ")",
                    TextContent = "模板SQL: " + (step.SqlText ?? "") + "\n执行SQL: " + sql + "\nOracle SQL: " + displaySql + "\n参数: " + dbgParamStr,
                    ConfigJson = "{}"
                });
            }

            if (!string.IsNullOrEmpty(step.ResultVar))
            {
                vars[step.ResultVar] = result;
                if (result.Type == "Scalar")
                {
                    ctx[step.ResultVar] = result.ScalarValue?.ToString() ?? "";
                    ctx[step.ResultVar + ".0"] = ctx[step.ResultVar];
                }
                else
                {
                    ctx[step.ResultVar] = $"({result.TableData.Rows.Count} 行)";
                    if (result.TableData.Rows.Count > 0 && result.TableData.Columns.Count > 0)
                        ctx[step.ResultVar + ".0"] = result.TableData.Rows[0][0]?.ToString() ?? "";
                }
            }
            if (!string.IsNullOrEmpty(step.ExpectOperator))
                CheckExpectation(step, result, ctx, stepList, ref idx, displays);
        }

        private ResultSet ExecuteSql(string sql, Dictionary<string, string> parameters, string dataSource)
        {
            var rs = new ResultSet { Type = "Table" };
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrEmpty(dataSource) || dataSource == "Config")
                {
                    var dbInit = new Repositories.DatabaseInitializer();
                    System.Data.SQLite.SQLiteParameter[] sqlParams = null;
                    if (parameters != null && parameters.Count > 0)
                    {
                        sqlParams = parameters.Select(kvp =>
                            new System.Data.SQLite.SQLiteParameter("@" + kvp.Key, (object)kvp.Value ?? System.DBNull.Value)).ToArray();
                    }
                    var dt = dbInit.Query(sql, sqlParams);
                    rs.TableData = dt;
                    if (dt.Rows.Count == 1 && dt.Columns.Count == 1)
                    { rs.Type = "Scalar"; rs.ScalarValue = dt.Rows[0][0]; }
                }
                else
                {
                    var executor = _executorFactory.GetExecutor(dataSource);
                    if (executor == null)
                        throw new Exception($"数据源 '{dataSource}' 未配置");

                    var dict = new Dictionary<string, object>();
                    foreach (var kvp in parameters)
                    {
                        dict["@" + kvp.Key] = string.IsNullOrEmpty(kvp.Value) ? null : kvp.Value;
                    }

                    DataTable dt = null;
                    try
                    {
                        dt = executor.ExecuteQuery(sql, dict);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Oracle 执行失败: {ex.Message}\nSQL: {sql}\nParams: {Newtonsoft.Json.JsonConvert.SerializeObject(dict)}");
                    }

                    rs.TableData = dt;
                    if (dt != null && dt.Rows.Count == 1 && dt.Columns.Count == 1)
                    { rs.Type = "Scalar"; rs.ScalarValue = dt.Rows[0][0]; }
                }
            }
            catch (Exception ex)
            {
                rs.Type = "Scalar";
                rs.ScalarValue = $"ERROR: {ex.Message}";
            }
            finally { if (rs.TableData == null) rs.TableData = new DataTable(); }

            sw.Stop();
            rs.ElapsedMs = sw.ElapsedMilliseconds;
            return rs;
        }

        private void CheckExpectation(FlowStep step, ResultSet result,
            Dictionary<string, string> ctx, List<FlowStep> stepList,
            ref int idx, List<DisplayBlock> displays)
        {
            bool ok = EvaluateExpectation(result, step);
            if (ok) return;

            switch (step.ExpectOnFail?.ToLower())
            {
                case "goto":
                    if (step.ExpectTarget.HasValue)
                    {
                        idx = stepList.FindIndex(s => s.SortOrder == step.ExpectTarget.Value);
                        if (idx < 0) idx = -1;
                        if (!string.IsNullOrEmpty(step.ExpectMessage))
                        {
                            var msg = TemplateEngine.ReplaceText(step.ExpectMessage, ctx);
                            displays.Add(new DisplayBlock { Type = "error", Title = step.Label ?? "数据诊断", TextContent = msg });
                        }
                    }
                    break;
                case "show_error":
                    var errMsg = TemplateEngine.ReplaceText(step.ExpectMessage ?? "数据检查未通过", ctx);
                    displays.Add(new DisplayBlock { Type = "error", Title = step.Label ?? "提示", TextContent = errMsg });
                    idx = -1;
                    break;
                case "stop": default: idx = -1; break;
            }
        }

        private bool EvaluateExpectation(ResultSet result, FlowStep step)
        {
            long actual;
            switch (step.ExpectOperator.ToLower())
            {
                case "gt": case "ge": case "lt": case "le":
                    actual = result.TableData?.Rows.Count ?? 0;
                    break;
                case "eq": case "ne": default:
                    if (result.Type == "Scalar" && result.ScalarValue != null)
                        long.TryParse(result.ScalarValue.ToString(), out actual);
                    else
                        actual = result.TableData?.Rows.Count ?? 0;
                    break;
            }
            long expected;
            if (!long.TryParse(step.ExpectValue, out expected)) return true;
            switch (step.ExpectOperator.ToLower())
            {
                case "gt": return actual > expected;
                case "ge": return actual >= expected;
                case "eq": return actual == expected;
                case "lt": return actual < expected;
                case "le": return actual <= expected;
                case "ne": return actual != expected;
                default: return true;
            }
        }

        private string BuildTextSummary(FlowStep step, ResultSet result, Dictionary<string, string> ctx)
        {
            if (!string.IsNullOrEmpty(step.DisplayConfig))
            {
                try
                {
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(step.DisplayConfig);
                    if (config.TryGetValue("template", out var template))
                        return TemplateEngine.ReplaceText(template, ctx);
                }
                catch { }
            }
            return result.Type == "Scalar" ? $"结果: {result.ScalarValue}" : $"共 {result.TableData?.Rows.Count ?? 0} 条记录";
        }
    }
}
