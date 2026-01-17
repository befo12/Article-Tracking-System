using System;
using System.Configuration;
using System.Data.SqlClient;

namespace WebApplication1.Pages
{
    public partial class ReaderPage : System.Web.UI.Page
    {
        int CurrentUserId => Session["UserId"] == null ? 0 : (int)Session["UserId"];
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        int ItemId
        {
            get
            {
                int.TryParse(Request.QueryString["id"], out var id);
                return id;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0)
            {
                Response.Redirect("~/Pages/login.aspx");
                return;
            }

            if (ItemId == 0)
            {
                Response.Redirect("~/Pages/library.aspx");
                return;
            }

            if (!IsPostBack)
                LoadItem();
        }

        void LoadItem()
        {
            string title = null, url = null;
            bool isRead = false;

            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand("SELECT Title, Url, IsRead FROM Library WHERE Id=@id AND UserId=@uid", con))
            {
                cmd.Parameters.AddWithValue("@id", ItemId);
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        title = r["Title"] as string;
                        url = r["Url"] as string;
                        isRead = Convert.ToBoolean(r["IsRead"]);
                    }
                }
            }

            if (url == null)
            {
                Response.Redirect("~/Pages/library.aspx");
                return;
            }

            litTitle.Text = Server.HtmlEncode(title);
            lnkOpen.NavigateUrl = url;
            lnkDownload.NavigateUrl = url;
            lnkDownload.Attributes["download"] = "";
            frame.Attributes["src"] = url;
            btnToggleRead.Text = isRead ? "Okunmadı Yap" : "Okundu Yap";
        }

        protected void btnToggleRead_Click(object sender, EventArgs e)
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand("UPDATE Library SET IsRead = 1 - IsRead WHERE Id=@id AND UserId=@uid", con))
            {
                cmd.Parameters.AddWithValue("@id", ItemId);
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            LoadItem();
        }
    }
}
