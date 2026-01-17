using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class LoginPage : Page
    {
        protected async void btnLogin_Click(object sender, EventArgs e)
        {
            await LoginProcessAsync();
        }

        private async Task LoginProcessAsync()
        {
            string email = txtEmail.Text.Trim();
            string pass = txtPassword.Text.Trim();

            if (email.Length == 0 || pass.Length == 0)
            {
                lblMsg.CssClass = "text-danger";
                lblMsg.Text = "E-posta ve şifre zorunlu.";
                return;
            }

            var payload = new { Email = email, Password = pass };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Sertifika doğrulama sorunu çözümü
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (sender, cert, chain, sslPolicyErrors) => true;

            using (var client = new HttpClient(handler))
            {
                var res = await client.PostAsync("https://localhost:44397/api/users/login", content);

                if (!res.IsSuccessStatusCode)
                {
                    lblMsg.CssClass = "text-danger";
                    lblMsg.Text = "Giriş hatalı.";
                    return;
                }

                // JSON body’yi al
                string body = await res.Content.ReadAsStringAsync();

                // JSON’u parse et
                dynamic data = JsonConvert.DeserializeObject(body);

                int userId = data.userId;
                string displayName = data.displayName;

                // Session bilgileri
                Session["UserId"] = userId;
                Session["displayName"] = displayName;
                Session["Email"] = email;

                // Authentication cookie
                FormsAuthentication.SetAuthCookie(email, false);

                // Dashboard’a yönlendir
                Response.Redirect("~/Pages/Dashboard.aspx", false);
            }
        }
    }
}
