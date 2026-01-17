using System;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class Default : Page
    {
        protected string Email { get { return Convert.ToString(Session["userEmail"] ?? "demo"); } }
        protected string Kws { get { return Convert.ToString(Session["keywords"] ?? ""); } }
        protected string Name { get { return Convert.ToString(Session["displayName"] ?? "Kullanıcı"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("~/Pages/Dashboard.aspx");
        }

    }
}
