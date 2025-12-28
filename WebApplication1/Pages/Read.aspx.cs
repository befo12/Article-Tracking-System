using System;
using System.Configuration;
using System.Data.SqlClient;

namespace WebApplication1.Pages
{
    public partial class Read : System.Web.UI.Page
    {
        int CurrentUserId => Session["UserId"] == null ? 0 : Convert.ToInt32(Session["UserId"]);
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        int ArticleId =>
            Request.QueryString["id"] == null ? 0 : Convert.ToInt32(Request.QueryString["id"]);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0)
            {
                Response.Redirect("~/Pages/Login.aspx");
                return;
            }

            if (ArticleId == 0)
            {
                Response.Redirect("~/Pages/Library.aspx");
                return;
            }

            if (!IsPostBack)
                LoadArticle();
        }

        private void LoadArticle()
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT Title, Note, IsRead, PdfPath, HtmlPath, Url FROM Library WHERE Id=@id AND UserId=@uid", con))
            {
                cmd.Parameters.AddWithValue("@id", ArticleId);
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);

                con.Open();
                var r = cmd.ExecuteReader();
                if (!r.Read()) return;

                lblTitle.Text = r["Title"].ToString();
                txtNote.Text = r["Note"].ToString();
                chkRead.Checked = Convert.ToBoolean(r["IsRead"]);

                string pdf = r["PdfPath"].ToString();
                string html = r["HtmlPath"].ToString();
                string url = r["Url"].ToString();

                if (!string.IsNullOrEmpty(pdf))
                {
                    phPdf.Visible = true;
                    pdfFrame.Attributes["src"] = "/SecureFile.ashx?id=" + ArticleId;
                }
                else if (!string.IsNullOrEmpty(html))
                {
                    phHtml.Visible = true;
                    htmlFrame.Attributes["src"] = "/SecureFile.ashx?id=" + ArticleId;
                }
                else
                {
                    phNone.Visible = true;
                    lnkOriginal.NavigateUrl = url;
                }
            }
        }

        protected void btnSaveNote_Click(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE Library SET Note=@n WHERE Id=@id AND UserId=@uid", con))
            {
                cmd.Parameters.AddWithValue("@n", (object)txtNote.Text ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", ArticleId);
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            lblStatus.Text = "Not başarıyla kaydedildi.";
        }

        protected void chkRead_CheckedChanged(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE Library SET IsRead=@r WHERE Id=@id AND UserId=@uid", con))
            {
                cmd.Parameters.AddWithValue("@r", chkRead.Checked);
                cmd.Parameters.AddWithValue("@id", ArticleId);
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            lblStatus.Text = "Okundu bilgisi güncellendi.";
        }
    }
}
