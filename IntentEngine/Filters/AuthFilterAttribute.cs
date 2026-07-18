using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace IntentEngine.Filters
{
    public class AuthFilterAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var controller = actionContext.ControllerContext.Controller;

            if (controller is Controllers.AuthController)
                return true;

            var session = HttpContext.Current?.Session;
            if (session != null && session["IsAuthenticated"] != null)
                return true;

            return false;
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.Request.CreateResponse(
                HttpStatusCode.OK,
                Models.ApiResponse.Fail("请先登录")
            );
        }
    }
}
