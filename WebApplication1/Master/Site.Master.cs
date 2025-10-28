using System;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebApplication1
{
    public partial class Site : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // LoginView içindeki Label'a isim/e-posta bas
            var lbl = lvAuth.FindControl("lblUser") as Label;
            if (lbl != null)
            {
                var display = Convert.ToString(Session["displayName"] ?? "");
                if (string.IsNullOrWhiteSpace(display) && Context?.User?.Identity != null)
                    display = Context.User.Identity.Name; // e-posta
                lbl.Text = display;
            }
        }

        protected void Logout_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Pages/Login.aspx?logout=1", true);
        }
    }
}
