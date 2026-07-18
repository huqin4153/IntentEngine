using System;
using System.Collections.Generic;
using System.Linq;
using IntentEngine.Contracts;
using IntentEngine.Models;

namespace IntentEngine.Services
{
    public class DefaultConfigService : IConfigService
    {
        private readonly IIntentRepository _intentRepo;
        private readonly IFunctionRepository _funcRepo;
        private readonly IFlowStepRepository _stepRepo;
        private readonly IFunctionParameterRepository _paramRepo;
        private readonly IEmbeddingService _embedding;
        private readonly IIntentMatcher _matcher;

        public DefaultConfigService(
            IIntentRepository intentRepo,
            IFunctionRepository funcRepo,
            IFlowStepRepository stepRepo,
            IFunctionParameterRepository paramRepo,
            IEmbeddingService embedding,
            IIntentMatcher matcher)
        {
            _intentRepo = intentRepo;
            _funcRepo = funcRepo;
            _stepRepo = stepRepo;
            _paramRepo = paramRepo;
            _embedding = embedding;
            _matcher = matcher;
        }

        public List<Intent> GetIntentTree()
        {
            var intents = _intentRepo.GetAll();
            foreach (var intent in intents)
            {
                intent.Functions = _funcRepo.GetByIntentId(intent.Id);
                foreach (var func in intent.Functions)
                {
                    func.Steps = _stepRepo.GetByFunctionId(func.Id);
                    func.Parameters = _paramRepo.GetByFunctionId(func.Id);
                }
            }
            return intents;
        }

        public void RebuildVectors()
        {
            _matcher.RebuildVectors();
        }

        public List<string> ValidateConfiguration()
        {
            var errors = new List<string>();
            var intents = _intentRepo.GetAll();

            foreach (var intent in intents)
            {
                if (string.IsNullOrWhiteSpace(intent.Name))
                    errors.Add($"意图 #{intent.Id}: 名称为空");

                var funcs = _funcRepo.GetByIntentId(intent.Id);
                if (funcs.Count == 0)
                {
                    errors.Add($"意图「{intent.Name}」: 没有配置任何 Function");
                    continue;
                }

                foreach (var func in funcs)
                {
                    var steps = _stepRepo.GetByFunctionId(func.Id);
                    if (steps.Count == 0)
                    {
                        errors.Add($"Function「{func.Name}」: 没有配置任何 FlowStep");
                        continue;
                    }

                    var sortOrders = new HashSet<int>(steps.Select(s => s.SortOrder));
                    foreach (var step in steps)
                    {
                        if (step.ExpectOnFail == "goto" && step.ExpectTarget.HasValue)
                        {
                            if (!sortOrders.Contains(step.ExpectTarget.Value))
                                errors.Add($"Function「{func.Name}」Step-{step.SortOrder}: goto 目标 {step.ExpectTarget} 不存在");
                        }

                        if (step.StepType == "sql" && string.IsNullOrWhiteSpace(step.SqlText))
                            errors.Add($"Function「{func.Name}」Step-{step.SortOrder}: SQL 为空");
                    }
                }
            }

            return errors;
        }

        public string ExportConfiguration()
        {
            var tree = GetIntentTree();
            return Newtonsoft.Json.JsonConvert.SerializeObject(tree, Newtonsoft.Json.Formatting.Indented);
        }

        public void ImportConfiguration(string json)
        {
            var intents = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Intent>>(json);
            if (intents == null) throw new Exception("JSON 格式无效");

            foreach (var intent in intents)
            {
                intent.Id = _intentRepo.Insert(intent);

                if (intent.Functions != null)
                {
                    foreach (var func in intent.Functions)
                    {
                        func.IntentId = intent.Id;
                        func.Id = _funcRepo.Insert(func);

                        if (func.Steps != null)
                        {
                            foreach (var step in func.Steps)
                            {
                                step.FunctionId = func.Id;
                                _stepRepo.Insert(step);
                            }
                        }

                        if (func.Parameters != null)
                        {
                            foreach (var param in func.Parameters)
                            {
                                param.FunctionId = func.Id;
                                _paramRepo.Insert(param);
                            }
                        }
                    }
                }
            }

            RebuildVectors();
        }
    }
}
