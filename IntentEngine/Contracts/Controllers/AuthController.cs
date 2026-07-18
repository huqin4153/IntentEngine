using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using IntentEngine.ConfigSections;
using IntentEngine.Models;

namespace IntentEngine.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost, Route("login")]
        public ApiResponse Login(LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return ApiResponse.Fail("请输入用户名和密码");

            var account = GetAccounts().FirstOrDefault(a =>
                a.Username == request.Username && a.Password == request.Password);

            if (account == null)
                return ApiResponse.Fail("用户名或密码错误");

            var session = HttpContext.Current?.Session;
            if (session == null)
                return ApiResponse.Fail("服务器 Session 不可用");

            session["IsAuthenticated"] = true;
            session["Username"] = account.Username;
            session["Role"] = account.Role;

            FormsAuthentication.SetAuthCookie(account.Username, false);

            return ApiResponse.Ok(new
            {
                username = account.Username,
                role = account.Role
            }, "登录成功");
        }

        [HttpPost, Route("logout")]
        public ApiResponse Logout()
        {
            HttpContext.Current.Session?.Abandon();
            FormsAuthentication.SignOut();
            return ApiResponse.Ok(null, "已退出");
        }

        [HttpGet, Route("status")]
        public ApiResponse GetStatus()
        {
            var session = HttpContext.Current?.Session;
            if (session?["IsAuthenticated"] == null)
                return ApiResponse.Fail("未登录");

            return ApiResponse.Ok(new
            {
                username = session["Username"]?.ToString(),
                role = session["Role"]?.ToString()
            }, "已登录");
        }

        private System.Collections.Generic.List<AccountInfo> GetAccounts()
        {
            var list = new System.Collections.Generic.List<AccountInfo>();

            var user = System.Configuration.ConfigurationManager.AppSettings["LoginUser"];
            var pass = System.Configuration.ConfigurationManager.AppSettings["LoginPass"];
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                list.Add(new AccountInfo { Username = user, Password = pass, Role = "管理员" });
            }

            try
            {
                var section = System.Configuration.ConfigurationManager.GetSection("loginAccounts") as LoginAccountSection;
                if (section != null)
                {
                    list.AddRange(section.GetAccounts());
                }
            }
            catch { }

            return list;
        }
    }
}
