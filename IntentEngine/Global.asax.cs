using System;
using System.Web;
using System.Web.Http;
using System.Web.SessionState;

namespace IntentEngine
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            IocConfig.Initialize();
            GlobalConfiguration.Configure(WebApiConfig.Register);

System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try { new Repositories.DatabaseInitializer().Initialize(); }
                catch { }
            });

}

        protected void Application_PostAuthorizeRequest()
        {
            var req = HttpContext.Current?.Request;
            if (req == null || !req.AppRelativeCurrentExecutionFilePath.StartsWith("~/api"))
                return;

            try
            {
                if (string.Equals(req.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(req.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Current.SetSessionStateBehavior(
                        SessionStateBehavior.ReadOnly);
                }
                else
                {
                    HttpContext.Current.SetSessionStateBehavior(
                        SessionStateBehavior.Required);
                }
            }
            catch { }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var ctx = HttpContext.Current;
            if (ctx?.Request.HttpMethod == "OPTIONS")
            {
                ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
                ctx.Response.AddHeader("Access-Control-Allow-Credentials", "true");
                ctx.Response.End();
            }
        }
    }
}
