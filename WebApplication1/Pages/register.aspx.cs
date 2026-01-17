using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class RegisterPage : Page
    {
        // 🚨 BU METOD ZORUNLU! OnClick bunu çağırır.
        protected async void btnRegister_Click(object sender, EventArgs e)
        {
            await RegisterProcessAsync();
        }

        private async Task RegisterProcessAsync()
        {
            string email = (txtEmail.Text ?? "").Trim();
            string pass = txtPassword.Text ?? "";
            string name = (txtName.Text ?? "").Trim();

            if (email.Length == 0 || pass.Length == 0)
            {
                lblMsg.CssClass = "text-danger";
                lblMsg.Text = "E-posta ve şifre zorunlu.";
                return;
            }

            var payload = new
            {
                Email = email,
                Password = pass,
                Name = name
            };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (sender, cert, chain, sslErrors) => true;

            using (var client = new HttpClient(handler))
            {
                var res = await client.PostAsync("https://localhost:44397/api/users/register", content);

                string apiMessage = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    lblMsg.CssClass = "text-danger";
                    lblMsg.Text = "Kayıt başarısız: " + apiMessage;
                    return;
                }

                lblMsg.CssClass = "text-success";
                lblMsg.Text = "Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz...";

                await Task.Delay(300);

                Response.Redirect("~/Pages/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }
    }
}
