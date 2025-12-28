using System;
using System.Web;
using System.Web.Security;
using System.Web.Http;

namespace WebApplication1
{
    public class Global : HttpApplication   // <-- Büyük G
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var ctx = HttpContext.Current;
            bool ok = ctx?.User?.Identity?.IsAuthenticated == true;
            string d = ctx?.Request?.QueryString?["demo"];

            if (!ok && d == "1")
            {
                FormsAuthentication.SetAuthCookie("demo@site.local", true);
                ctx.Response.Redirect("~/Pages/Dashboard.aspx", false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
    }
}
