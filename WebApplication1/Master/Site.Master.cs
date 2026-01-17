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
            // Eğer session yoksa, LoginView zaten AnonymousTemplate gösterecektir.
            if (Session["Email"] != null)
            {
                var lbl = lvAuth.FindControl("lblUser") as Label;
                if (lbl != null)
                {
                    // Önce displayName'e bak, yoksa email'i yaz
                    var display = Convert.ToString(Session["displayName"] ?? "");
                    if (string.IsNullOrWhiteSpace(display))
                    {
                        display = Convert.ToString(Session["Email"]);
                    }
                    lbl.Text = display;
                }
            }
        }

        protected void Logout_Click(object sender, EventArgs e)
        {
            // Tüm güvenlik verilerini temizle
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            // Çıkış yapınca sayfayı yenile ve login'e at
            Response.Redirect("~/Pages/Login.aspx?logout=1", true);
        }
    }
}