using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Security;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class LoginPage : Page
    {
        private string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            lblDbg.Text = (IsPostBack ? "POSTBACK: true" : "POSTBACK: false")
                          + " | Method=" + Request.HttpMethod
                          + " | FormKeys=" + Request.Form.Count;

            if (!IsPostBack && Request.QueryString["logout"] == "1")
            {
                lblMsg.CssClass = "text-muted";
                lblMsg.Text = "Çıkış yapıldı.";
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            DoLogin();
        }

        private void DoLogin()
        {
            lblMsg.CssClass = "text-danger d-block mb-2";
            var email = (txtEmail.Text ?? "").Trim();
            var pass = txtPassword.Text ?? "";

            try
            {
                // DEMO kullanıcı
                if (email.Equals("demo@site.local", StringComparison.OrdinalIgnoreCase) && pass == "123456")
                {
                    AfterAuthSuccess(1, "Demo Kullanıcı", email);
                    return;
                }

                using (var con = new SqlConnection(Cs))
                using (var cmd = new SqlCommand(
                    "SELECT Id, ISNULL(Name,'') AS Name FROM Users WHERE Email=@e AND Password=@p", con))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@p", pass); // MVP: düz metin
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            int uid = Convert.ToInt32(r["Id"]);
                            string name = Convert.ToString(r["Name"]);
                            AfterAuthSuccess(uid, name, email);
                            return;
                        }
                    }
                }

                lblMsg.Text = "E-posta veya şifre hatalı.";
            }
            catch (Exception ex)
            {
                lblMsg.Text = "Giriş hatası: " + Server.HtmlEncode(ex.Message);
            }
        }
        private void AfterAuthSuccess(int userId, string displayName, string email)
        {
            Session["UserId"] = userId;
            Session["Email"] = (email ?? string.Empty).Trim(); // <-- user.Email değil, parametre email
            Session["displayName"] = displayName;

            // LoginView'ın Auth template'ine geçmesi için
            FormsAuthentication.SetAuthCookie(email ?? string.Empty, false);

            SafeRedirect("~/Pages/Dashboard.aspx");
        }


        private void SafeRedirect(string url)
        {
            Response.Redirect(ResolveUrl(url), false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
