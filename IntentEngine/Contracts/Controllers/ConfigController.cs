using System;
using System.Linq;
using System.Web.Http;
using IntentEngine.Models;

namespace IntentEngine.Controllers
{
    [RoutePrefix("api/config")]
    public class ConfigController : ApiController
    {
        [HttpGet, Route("tree")]
        public ApiResponse GetTree()
        {
            var service = IocConfig.Container.GetConfigService();
            var tree = service.GetIntentTree();
            return ApiResponse.Ok(tree.Select(i => new
            {
                i.Id, i.Name, i.Description, i.Keywords, i.Category, i.SemanticText, i.IsActive,
                functions = i.Functions?.Select(f => new
                {
                    f.Id, f.Name, f.Description, f.SortOrder, f.DataSource,
                    steps = f.Steps?.OrderBy(s => s.SortOrder).Select(s => new
                    {
                        s.Id, s.SortOrder, s.StepType, s.Label, s.SqlText, s.ResultVar,
                        s.ExpectOperator, s.ExpectValue, s.ExpectOnFail, s.ExpectTarget, s.ExpectMessage,
                        s.DisplayTitle, s.DisplaySource, s.DisplayConfig, s.DataSource, s.IsEnd
                    }),
                    parameters = f.Parameters?.OrderBy(p => p.SortOrder).Select(p => new
                    {
                        p.Id, p.Name, p.Label, p.ControlType, p.DataSource, p.IsRequired, p.DefaultValue
                    })
                })
            }));
        }

        [HttpGet, Route("detail")]
        public ApiResponse GetDetail(string type, int id)
        {
            switch (type?.ToLower())
            {
                case "intent":
                    var intent = IocConfig.Container.GetIntentRepo().GetById(id);
                    if (intent == null) return ApiResponse.Fail("意图不存在");
                    return ApiResponse.Ok(new
                    {
                        intent.Id, intent.Name, intent.Description, intent.Keywords,
                        intent.Category, intent.SemanticText, intent.IsActive
                    });

                case "function":
                    var func = IocConfig.Container.GetFunctionRepo().GetById(id);
                    if (func == null) return ApiResponse.Fail("功能不存在");
                    return ApiResponse.Ok(new { func.Id, func.IntentId, func.Name, func.Description, func.SortOrder, func.DataSource });

                case "step":
                    var step = IocConfig.Container.GetFlowStepRepo().GetById(id);
                    if (step == null) return ApiResponse.Fail("步骤不存在");
                    return ApiResponse.Ok(step);

                case "parameter":
                    var param = IocConfig.Container.GetParamRepo().GetById(id);
                    if (param == null) return ApiResponse.Fail("参数不存在");
                    return ApiResponse.Ok(param);

                default:
                    return ApiResponse.Fail("未知类型");
            }
        }

        [HttpPost, Route("save")]
        public ApiResponse Save(ConfigSaveRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Type))
                return ApiResponse.Fail("参数错误");

            try
            {
                switch (request.Type.ToLower())
                {
                    case "intent":
                        var intent = Newtonsoft.Json.JsonConvert.DeserializeObject<Intent>(request.Data.ToString());
                        if (intent.Id > 0)
                            IocConfig.Container.GetIntentRepo().Update(intent);
                        else
                            intent.Id = IocConfig.Container.GetIntentRepo().Insert(intent);
                        return ApiResponse.Ok(new { intent.Id }, "保存成功");

                    case "function":
                        var func = Newtonsoft.Json.JsonConvert.DeserializeObject<Function>(request.Data.ToString());
                        if (func.Id > 0)
                            IocConfig.Container.GetFunctionRepo().Update(func);
                        else
                            func.Id = IocConfig.Container.GetFunctionRepo().Insert(func);
                        return ApiResponse.Ok(new { func.Id }, "保存成功");

                    case "step":
                        var step = Newtonsoft.Json.JsonConvert.DeserializeObject<FlowStep>(request.Data.ToString());
                        if (step.Id > 0)
                            IocConfig.Container.GetFlowStepRepo().Update(step);
                        else
                            step.Id = IocConfig.Container.GetFlowStepRepo().Insert(step);
                        return ApiResponse.Ok(new { step.Id }, "保存成功");

                    case "parameter":
                        var param = Newtonsoft.Json.JsonConvert.DeserializeObject<FunctionParameter>(request.Data.ToString());
                        if (param.Id > 0)
                            IocConfig.Container.GetParamRepo().Update(param);
                        else
                            param.Id = IocConfig.Container.GetParamRepo().Insert(param);
                        return ApiResponse.Ok(new { param.Id }, "保存成功");

                    default:
                        return ApiResponse.Fail("未知类型");
                }
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"保存失败: {ex.Message}");
            }
        }

        [HttpPost, Route("delete")]
        public ApiResponse Delete(ConfigDeleteRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Type))
                return ApiResponse.Fail("参数错误");

            try
            {
                switch (request.Type.ToLower())
                {
                    case "intent":
                        var funcs = IocConfig.Container.GetFunctionRepo().GetByIntentId(request.Id);
                        foreach (var f in funcs)
                        {
                            var steps = IocConfig.Container.GetFlowStepRepo().GetByFunctionId(f.Id);
                            foreach (var s in steps) IocConfig.Container.GetFlowStepRepo().Delete(s.Id);
                            var pars = IocConfig.Container.GetParamRepo().GetByFunctionId(f.Id);
                            foreach (var p in pars) IocConfig.Container.GetParamRepo().Delete(p.Id);
                            IocConfig.Container.GetFunctionRepo().Delete(f.Id);
                        }
                        IocConfig.Container.GetIntentRepo().Delete(request.Id);
                        break;
                    case "function":
                        var delSteps = IocConfig.Container.GetFlowStepRepo().GetByFunctionId(request.Id);
                        foreach (var s in delSteps) IocConfig.Container.GetFlowStepRepo().Delete(s.Id);
                        var delPars = IocConfig.Container.GetParamRepo().GetByFunctionId(request.Id);
                        foreach (var p in delPars) IocConfig.Container.GetParamRepo().Delete(p.Id);
                        IocConfig.Container.GetFunctionRepo().Delete(request.Id);
                        break;
                    case "step":
                        IocConfig.Container.GetFlowStepRepo().Delete(request.Id);
                        break;
                    case "parameter":
                        IocConfig.Container.GetParamRepo().Delete(request.Id);
                        break;
                    default:
                        return ApiResponse.Fail("未知类型");
                }
                return ApiResponse.Ok(null, "删除成功");
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"删除失败: {ex.Message}");
            }
        }

        [HttpPost, Route("rebuildVectors")]
        public ApiResponse RebuildVectors()
        {
            try
            {
                IocConfig.Container.GetConfigService().RebuildVectors();
                return ApiResponse.Ok(null, "向量重建完成");
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"重建失败: {ex.Message}");
            }
        }

        [HttpGet, Route("validate")]
        public ApiResponse Validate()
        {
            var errors = IocConfig.Container.GetConfigService().ValidateConfiguration();
            if (errors.Count == 0)
                return ApiResponse.Ok(null, "配置验证通过");
            return ApiResponse.Ok(errors, $"发现 {errors.Count} 个问题");
        }

        [HttpPost, Route("export")]
        public ApiResponse Export()
        {
            var json = IocConfig.Container.GetConfigService().ExportConfiguration();
            return ApiResponse.Ok(new { json });
        }

        [HttpPost, Route("import")]
        public ApiResponse Import(ImportRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Json))
                return ApiResponse.Fail("请提供 JSON 数据");

            try
            {
                IocConfig.Container.GetConfigService().ImportConfiguration(request.Json);
                return ApiResponse.Ok(null, "导入成功");
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"导入失败: {ex.Message}");
            }
        }

        public class ConfigSaveRequest
        {
            public string Type { get; set; }
            public object Data { get; set; }
        }

        public class ConfigDeleteRequest
        {
            public string Type { get; set; }
            public int Id { get; set; }
        }

        public class ImportRequest
        {
            public string Json { get; set; }
        }
    }
}
