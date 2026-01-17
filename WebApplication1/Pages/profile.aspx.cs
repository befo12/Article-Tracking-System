using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

namespace WebApplication1.Pages
{
    public partial class ProfilePage : Page
    {
        // SSL Sertifika hatasını görmezden gelen Handler ile HttpClient tanımı
        private static readonly HttpClient client = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        });

        private string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;
        private int CurrentUserId => Session["UserId"] == null ? 0 : (int)Session["UserId"];

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0) { Response.Redirect("~/Pages/login.aspx"); return; }

            if (!IsPostBack)
            {
                LoadUserInfo();
                LoadUserStats();
                // Web Forms async görev yönetimi
                RegisterAsyncTask(new PageAsyncTask(LoadKeywords));
            }
        }

        private async Task LoadKeywords()
        {
            try
            {
                string apiUrl = $"https://localhost:44397/api/keywords/get?userId={CurrentUserId}"; var res = await client.GetAsync(apiUrl);

                if (res.IsSuccessStatusCode)
                {
                    string json = await res.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<KeywordDto>>(json);
                    rptKeywords.DataSource = list ?? new List<KeywordDto>();
                    rptKeywords.DataBind();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("API Get Error: " + ex.Message); }
        }

        protected async void btnAddKeyword_Click(object sender, EventArgs e)
        {
            string k = txtKeyword.Text.Trim();
            if (string.IsNullOrEmpty(k)) return;

            try
            {
                string apiUrl = $"https://localhost:44397/api/keywords/add?userId={CurrentUserId}"; var content = new StringContent(JsonConvert.SerializeObject(k), Encoding.UTF8, "application/json");

                var res = await client.PostAsync(apiUrl, content);
                if (res.IsSuccessStatusCode)
                {
                    txtKeyword.Text = "";
                    // Listeyi hemen tazelemek için görevi tekrar çağır
                    await LoadKeywords();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("API Post Error: " + ex.Message); }
        }

        protected async void rptKeywords_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Delete") return;

            try
            {
                string apiUrl = $"https://localhost:44397/api/keywords/delete/{e.CommandArgument}?userId={CurrentUserId}"; var res = await client.DeleteAsync(apiUrl);
                if (res.IsSuccessStatusCode) await LoadKeywords();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("API Delete Error: " + ex.Message); }
        }

        private void LoadUserInfo()
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand("SELECT Email, Name FROM Users WHERE Id=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", CurrentUserId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        litEmail.Text = r["Email"]?.ToString();
                        string name = r["Name"]?.ToString();
                        litName.Text = string.IsNullOrWhiteSpace(name) ? litEmail.Text : name;
                    }
                }
            }
        }

        private void LoadUserStats()
        {
            using (var con = new SqlConnection(Cs))
            {
                con.Open();

                // Sadece kullanıcının 5 puan verdiği makaleleri say (Library ve ArticleRatings birleştirilmeli)
                // Eğer puanlar Library tablosunda değilse, ArticleRatings tablosu üzerinden sayıyoruz:
                string favSql = @"SELECT COUNT(*) FROM ArticleRatings 
                          WHERE UserId = @u AND Rating = 5";

                litFavCount.Text = GetCount(con, favSql);

                // Diğer istatistikleri de veritabanından çekiyoruz
                litSearchCount.Text = GetCount(con, "SELECT COUNT(*) FROM SearchHistory WHERE UserId=@u");
                litRateCount.Text = GetCount(con, "SELECT COUNT(*) FROM ArticleRatings WHERE UserId=@u");
            }
        }

        private string GetCount(SqlConnection con, string sql)
        {
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                object res = cmd.ExecuteScalar();
                return res != null ? res.ToString() : "0";
            }
        }

        public class KeywordDto { public int Id { get; set; } public string Keyword { get; set; } }
    }
}