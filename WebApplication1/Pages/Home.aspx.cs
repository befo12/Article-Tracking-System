using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class Home : Page
    {
        private string Cs
        {
            get { return ConfigurationManager.ConnectionStrings["Db"].ConnectionString; }
        }
        private int CurrentUserId
        {
            get { return Session["UserId"] == null ? 0 : (int)Session["UserId"]; }
        }

        protected string Name { get; private set; }
        protected string Email { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0) { Response.Redirect("~/Pages/login.aspx"); return; }
            if (!IsPostBack)
            {
                LoadUserInfo();
                BindActivity();
                // İstersen burada BindWeeklySuggestions() da çağırırsın
            }
        }

        private void LoadUserInfo()
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand("SELECT Email, Name FROM Users WHERE Id=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", CurrentUserId);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        Email = Convert.ToString(r["Email"]);
                        Name = Convert.ToString(r["Name"]);
                        if (string.IsNullOrWhiteSpace(Name)) Name = Email;
                    }
                }
            }
        }
        protected void btnQuickAdd_Click(object sender, EventArgs e)
        {
            string title = (txtQuickTitle.Text ?? "").Trim();
            string url = (txtQuickUrl.Text ?? "").Trim();
            string note = (txtQuickNote.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
            {
                lblQuickMsg.ForeColor = System.Drawing.Color.Red;
                lblQuickMsg.Text = "Başlık ve URL zorunlu.";
                return;
            }

            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM Library WHERE UserId=@u AND Url=@url)
BEGIN
    INSERT INTO Library(UserId, Title, Url, Note, IsRead)
    VALUES(@u, @t, @url, NULLIF(@n,''), 0);
END", con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                cmd.Parameters.AddWithValue("@t", title);
                cmd.Parameters.AddWithValue("@url", url);
                cmd.Parameters.AddWithValue("@n", note);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            // temizlik ve mesaj
            txtQuickTitle.Text = "";
            txtQuickUrl.Text = "";
            txtQuickNote.Text = "";
            lblQuickMsg.ForeColor = System.Drawing.Color.Green;
            lblQuickMsg.Text = "Kütüphaneye eklendi.";

            // İstersen etkinlik akışını yenile:
            BindActivity();
        }

        private void BindActivity()
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP 10 Title AS [Title], AddedDate AS [When], 'Siz' AS [User],
       'Kütüphaneye eklendi: ' + Title AS [Text]
FROM Library
WHERE UserId=@u
ORDER BY [When] DESC", con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                con.Open();
                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                rptFeed.DataSource = dt;
                rptFeed.DataBind();
            }
        }
    }
}
