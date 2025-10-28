using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebApplication1.Pages
{
    public partial class ProfilePage : Page
    {
        private string Cs { get { return ConfigurationManager.ConnectionStrings["Db"].ConnectionString; } }
        private int CurrentUserId { get { return Session["UserId"] == null ? 0 : (int)Session["UserId"]; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0) { Response.Redirect("~/Pages/login.aspx"); return; }
            if (!IsPostBack) { LoadUser(); BindKeywords(); }
        }

        private void LoadUser()
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand("SELECT Email, Name FROM Users WHERE Id=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", CurrentUserId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        litEmail.Text = Convert.ToString(r["Email"]);
                        string n = Convert.ToString(r["Name"]);
                        litName.Text = string.IsNullOrWhiteSpace(n) ? litEmail.Text : n;
                    }
                }
            }
        }

        private void BindKeywords()
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand("SELECT Id, Keyword FROM UserKeywords WHERE UserId=@u ORDER BY Keyword", con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                con.Open();
                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                gvKeywords.DataSource = dt;
                gvKeywords.DataBind();
            }
        }

        protected void btnAddKeyword_Click(object sender, EventArgs e)
        {
            string k = (txtKeyword.Text ?? "").Trim();
            if (k.Length == 0) return;

            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(
                "IF NOT EXISTS (SELECT 1 FROM UserKeywords WHERE UserId=@u AND Keyword=@k) INSERT INTO UserKeywords(UserId,Keyword) VALUES(@u,@k)", con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                cmd.Parameters.AddWithValue("@k", k);
                con.Open(); cmd.ExecuteNonQuery();
            }
            txtKeyword.Text = "";
            BindKeywords();
        }

        protected void gvKeywords_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Del") return;
            int idx = Convert.ToInt32(e.CommandArgument);
            int id = (int)gvKeywords.DataKeys[idx].Values["Id"];

            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand("DELETE FROM UserKeywords WHERE Id=@id AND UserId=@u", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                con.Open(); cmd.ExecuteNonQuery();
            }
            BindKeywords();
        }
    }
}
