using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class RegisterPage : Page
    {
        private string Cs { get { return ConfigurationManager.ConnectionStrings["Db"].ConnectionString; } }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            var email = (txtEmail.Text ?? "").Trim();
            var password = txtPassword.Text ?? "";
            var name = (txtName.Text ?? "").Trim();

            if (email.Length == 0 || password.Length == 0)
            {
                lblMsg.CssClass = "text-danger";
                lblMsg.Text = "E-posta ve şifre zorunlu.";
                return;
            }

            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM Users WHERE Email=@e) SELECT -1; " +
                "ELSE BEGIN INSERT INTO Users(Email,Password,Name) VALUES(@e,@p,NULLIF(@n,'')); SELECT SCOPE_IDENTITY(); END", con))
            {
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@p", password); // MVP: düz metin
                cmd.Parameters.AddWithValue("@n", (object)name ?? DBNull.Value);
                con.Open();
                var res = cmd.ExecuteScalar();
                if (res is int && (int)res == -1)
                {
                    lblMsg.CssClass = "text-danger";
                    lblMsg.Text = "Bu e-posta zaten kayıtlı.";
                    return;
                }
            }

            lblMsg.CssClass = "text-success";
            lblMsg.Text = "Kayıt başarılı. Giriş sayfasına yönlendiriliyorsunuz...";
            Response.Redirect(ResolveUrl("~/Pages/Login.aspx"), true);
        }
    }
}
