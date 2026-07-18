<%@ Page Language="C#" %>
<%
    string user = Request.Form["username"];
    string pass = Request.Form["password"];
    string captcha = Request.Form["captcha"];
    string cfgUser = System.Configuration.ConfigurationManager.AppSettings["LoginUser"] ?? "admin";
    string cfgPass = System.Configuration.ConfigurationManager.AppSettings["LoginPass"] ?? "admin123";

    string err = "";
    object sessionCaptchaObj = Session["Captcha"];
    string sessionCaptcha = (sessionCaptchaObj != null) ? sessionCaptchaObj.ToString() : "";
    if (!string.IsNullOrEmpty(sessionCaptcha))
        Session.Remove("Captcha"); // 用完即删

    if (string.IsNullOrEmpty(captcha) || string.IsNullOrEmpty(sessionCaptcha) ||
        captcha.ToUpperInvariant() != sessionCaptcha.ToUpperInvariant())
    {
        err = "captcha=1";
    }

    if (string.IsNullOrEmpty(err))
    {
        if (user == cfgUser && pass == cfgPass)
        {
            Session["IsAuthenticated"] = true;
            Session["Username"] = user;
            Session["Role"] = "管理员";
            System.Web.Security.FormsAuthentication.SetAuthCookie(user, false);
            Response.Redirect("Default.aspx");
        }
        else
        {
            err = "error=1";
        }
    }

    Response.Redirect("Default.aspx?" + err);
%>
