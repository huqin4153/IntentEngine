using System;
using System.Web.Http;
using IntentEngine.Models;

namespace IntentEngine.Controllers
{
    [RoutePrefix("api/system")]
    public class SystemController : ApiController
    {
        [HttpGet, Route("status")]
        public ApiResponse GetStatus()
        {
            var embedding = IocConfig.Container.GetEmbeddingService();
            try { new Repositories.DatabaseInitializer().Initialize(); } catch { }

            return ApiResponse.Ok(new
            {
                embedding = new
                {
                    model = embedding.ModelName,
                    ready = embedding.IsReady,
                    onnxError = IocConfig.OnnxInitError
                },
                intents = new
                {
                    count = embedding.IsReady
                        ? IocConfig.Container.GetIntentRepo().GetActive().Count
                        : 0
                },
                version = "1.0.0",
                framework = ".NET Framework 4.5",
                database = "SQLite"
            });
        }

        [HttpGet, Route("oracletest")]
        public ApiResponse OracleTest()
        {
            try
            {
                var factory = IocConfig.Container.GetExecutorFactory();
                var exec = factory.GetExecutor("BusinessDB");
                if (exec == null)
                    return ApiResponse.Fail("BusinessDB 数据源未配置");

                var results = new System.Collections.Generic.List<object>();

                try
                {
                    var dt1 = exec.ExecuteQuery("SELECT 1 AS val FROM DUAL", null);
                    var val = dt1.Rows.Count > 0 ? dt1.Rows[0][0]?.ToString() : "null";
                    results.Add(new { test = "SELECT 1 FROM DUAL", result = val, rows = dt1.Rows.Count });
                }
                catch (Exception ex)
                {
                    results.Add(new { test = "SELECT 1 FROM DUAL", error = ex.Message });
                }

                try
                {
                    var dt2 = exec.ExecuteQuery("SELECT COUNT(*) AS cnt FROM dual", null);
                    var cnt = dt2.Rows.Count > 0 ? dt2.Rows[0][0]?.ToString() : "0";
                    results.Add(new { test = "parameterized query", result = cnt });
                }
                catch (Exception ex)
                {
                    results.Add(new { test = "table query", error = ex.Message });
                }

                return ApiResponse.Ok(new
                {
                    provider = exec.ProviderName,
                    connector = exec.GetType().Name,
                    tests = results
                });
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"测试失败: {ex.Message}");
            }
        }

        [HttpGet, Route("embedding/models")]
        public ApiResponse GetAvailableModels()
        {
            return ApiResponse.Ok(new object[]
            {
                new { id = "char-embed-v1", name = "纯 C# 字符语义嵌入", type = "built-in", required = (object)null },
                new { id = "bge-small-zh", name = "BGE-small-zh (ONNX)", type = "optional",
                      required = (object)new { onnxruntime = "onnxruntime.dll", model = "Resources/bge-small-zh-v1.5.onnx" } }
            });
        }

        [HttpGet, Route("onnxdiag")]
        public ApiResponse OnnxDiagnostic()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var diag = new System.Collections.Generic.Dictionary<string, object>();

                string dllPath1 = System.IO.Path.Combine(baseDir, "onnxruntime.dll");
                string dllPath2 = System.IO.Path.Combine(baseDir, "bin", "onnxruntime.dll");
                string modelPath = System.IO.Path.Combine(baseDir, "Resources", "bge-small-zh-v1.5.onnx");
                diag["baseDir"] = baseDir;
                diag["dll_at_root_exists"] = System.IO.File.Exists(dllPath1);
                diag["dll_at_bin_exists"] = System.IO.File.Exists(dllPath2);
                diag["model_exists"] = System.IO.File.Exists(modelPath);

                if (System.IO.File.Exists(dllPath1))
                {
                    try
                    {
                        var h = IntentEngine.Services.OnnxRuntimeNative.CheckHealth();
                        diag["loadlibrary_result"] = h ? "成功" : "失败（OrtGetApiBase返回空）";
                    }
                    catch (System.Exception ex)
                    {
                        diag["loadlibrary_error"] = ex.GetType().Name + ": " + ex.Message;
                    }
                }
                else
                {
                    diag["loadlibrary_result"] = "未尝试（文件不存在）";
                }

                diag["is_64bit_process"] = System.Environment.Is64BitProcess;
                diag["platform"] = System.Environment.OSVersion.ToString();

                return ApiResponse.Ok(diag);
            }
            catch (System.Exception ex)
            {
                return ApiResponse.Fail($"诊断失败: {ex.Message}");
            }
        }
    }
}
