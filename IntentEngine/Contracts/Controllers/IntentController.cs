using System;
using System.Linq;
using System.Web.Http;
using IntentEngine.Models;

namespace IntentEngine.Controllers
{
    [RoutePrefix("api/intent")]
    public class IntentController : ApiController
    {
        public class MatchRequest
        {
            public string Text { get; set; }
        }

        [HttpPost, Route("match")]
        public ApiResponse Match(MatchRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return ApiResponse.Fail("请输入查询内容");

            if (request.Text.StartsWith("#"))
            {
                var cmd = request.Text.ToLower().Trim();
                if (cmd == "#help" || cmd == "#帮助" || cmd == "#?")
                {
                    var intents = IocConfig.Container.GetIntentRepo().GetActive();
                    return ApiResponse.Ok(intents.Select(i => new { i.Name, i.Category, i.Description }), "输入 #退出 退出系统");
                }
                if (cmd == "#exit" || cmd == "#退出" || cmd == "#quit")
                {
                    return ApiResponse.Ok(new { action = "exit" }, "确认退出");
                }
                if (cmd == "#clear" || cmd == "#清除")
                {
                    return ApiResponse.Ok(new { action = "clear" }, "已清除");
                }
                return ApiResponse.Fail("未知指令。支持: #帮助, #退出, #清除");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var matcher = IocConfig.Container.GetIntentMatcher();
            var results = matcher.Match(request.Text, 5);
            sw.Stop();

            var embedding = IocConfig.Container.GetEmbeddingService();
            var data = results.Select(r => new
            {
                intent = new
                {
                    id = r.Intent.Id,
                    name = r.Intent.Name,
                    description = r.Intent.Description,
                    category = r.Intent.Category
                },
                similarity = Math.Round(r.Similarity * 100),
                confidence = r.ConfidenceLevel,
                isFallback = r.IsFallback
            });

            return ApiResponse.Ok(new { results = data, elapsedMs = sw.ElapsedMilliseconds });
        }

        [HttpGet, Route("list")]
        public ApiResponse GetAll()
        {
            var intents = IocConfig.Container.GetIntentRepo().GetActive();
            return ApiResponse.Ok(intents.Select(i => new
            {
                i.Id, i.Name, i.Description, i.Category
            }));
        }
    }
}
