using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WebApplication1.Pages
{
    public partial class Library : System.Web.UI.Page
    {
        int CurrentUserId => Session["UserId"] == null ? 0 : Convert.ToInt32(Session["UserId"]);
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0) { Response.Redirect("~/Pages/Login.aspx"); return; }
            if (!IsPostBack) BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { gvLib.PageIndex = 0; BindGrid(); }

        protected void gvLib_PageIndexChanging(object sender, GridViewPageEventArgs e) { gvLib.PageIndex = e.NewPageIndex; BindGrid(); }

        protected void gvLib_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var drv = (DataRowView)e.Row.DataItem;
                var ddl = (DropDownList)e.Row.FindControl("ddlRating");
                if (ddl != null && drv["Rating"] != DBNull.Value)
                    ddl.SelectedValue = drv["Rating"].ToString();
            }
        }

        protected void RatingChanged(object sender, EventArgs e)
        {
            var ddl = (DropDownList)sender;
            var row = (GridViewRow)ddl.NamingContainer;
            int libraryId = Convert.ToInt32(gvLib.DataKeys[row.RowIndex].Value);
            int rating = Convert.ToInt32(ddl.SelectedValue);

            using (SqlConnection con = new SqlConnection(Cs))
            {
                con.Open();
                string sql = @"
                    IF EXISTS (SELECT 1 FROM ArticleRatings WHERE UserId=@uid AND LibraryId=@lid)
                        UPDATE ArticleRatings SET Rating=@r, RatedDate=GETDATE() WHERE UserId=@uid AND LibraryId=@lid
                    ELSE
                        INSERT INTO ArticleRatings (UserId, LibraryId, Rating) VALUES (@uid, @lid, @r)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                cmd.Parameters.AddWithValue("@lid", libraryId);
                cmd.Parameters.AddWithValue("@r", rating == 0 ? (object)DBNull.Value : rating);
                cmd.ExecuteNonQuery();

                if (rating >= 4)
                {
                    string fav = "IF NOT EXISTS (SELECT 1 FROM Favorites WHERE UserId=@uid AND LibraryId=@lid) INSERT INTO Favorites (UserId, LibraryId) VALUES (@uid, @lid)";
                    using (SqlCommand fCmd = new SqlCommand(fav, con))
                    {
                        fCmd.Parameters.AddWithValue("@uid", CurrentUserId);
                        fCmd.Parameters.AddWithValue("@lid", libraryId);
                        fCmd.ExecuteNonQuery();
                    }
                }
            }
            // Başarı mesajını BindGrid'e gönderiyoruz ki ezilmesin
            BindGrid("Puan başarıyla kaydedildi.");
        }

        // BindGrid metoduna isteğe bağlı mesaj parametresi eklendi
        private void BindGrid(string customMessage = null)
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = con;
                var where = "WHERE L.UserId=@uid";
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);

                if (!string.IsNullOrEmpty(ddlRead.SelectedValue))
                {
                    where += " AND L.IsRead=@r";
                    cmd.Parameters.Add("@r", SqlDbType.Bit).Value = ddlRead.SelectedValue == "1";
                }

                string raw = (txtSearch.Text ?? "").Trim();
                var tokens = Regex.Split(raw, @"\s+").Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                if (tokens.Count > 0)
                {
                    var clauses = new List<string>();
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        string p = "@t" + i;
                        cmd.Parameters.AddWithValue(p, "%" + tokens[i] + "%");
                        string scope = ddlSearchIn.SelectedValue;
                        clauses.Add(scope == "title" ? $"L.Title LIKE {p}" : scope == "note" ? $"L.Note LIKE {p}" : $"(L.Title LIKE {p} OR L.Note LIKE {p})");
                    }
                    where += " AND (" + string.Join(ddlMatch.SelectedValue == "OR" ? " OR " : " AND ", clauses) + ")";
                }

                cmd.CommandText = $@"
                    SELECT L.*, R.Rating 
                    FROM Library L 
                    LEFT JOIN ArticleRatings R ON L.Id = R.LibraryId AND L.UserId = R.UserId
                    {where} ORDER BY L.AddedDate DESC";

                var dt = new DataTable();
                con.Open();
                dt.Load(cmd.ExecuteReader());
                gvLib.DataSource = dt;
                gvLib.DataBind();

                // Eğer özel mesaj varsa onu göster, yoksa kayıt sayısını göster
                if (!string.IsNullOrEmpty(customMessage))
                {
                    lblInfo.Text = customMessage;
                    lblInfo.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblInfo.Text = $"{dt.Rows.Count} kayıt listelendi.";
                    lblInfo.ForeColor = System.Drawing.Color.Empty; // Varsayılan renk
                }
            }
        }

        protected void gvLib_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "remove")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                using (var con = new SqlConnection(Cs))
                {
                    con.Open();
                    string sql = "DELETE FROM Library WHERE Id=@id AND UserId=@uid";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                BindGrid("Makale kütüphaneden çıkartıldı.");
            }
            else if (e.CommandName == "saveNote")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                var row = (GridViewRow)((LinkButton)e.CommandSource).NamingContainer;
                string note = ((TextBox)row.FindControl("txtNote")).Text.Trim();
                using (var con = new SqlConnection(Cs))
                {
                    con.Open();
                    string sql = "UPDATE Library SET Note=@n WHERE Id=@id AND UserId=@uid";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@n", note);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                // Not kaydedildikten sonra geri bildirim veriyoruz
                BindGrid("Not başarıyla güncellendi.");
            }
        }

        protected void ChkReadChanged(object sender, EventArgs e)
        {
            var chk = (CheckBox)sender;
            int id = Convert.ToInt32(gvLib.DataKeys[((GridViewRow)chk.NamingContainer).RowIndex].Value);
            using (var con = new SqlConnection(Cs))
            {
                con.Open();
                string sql = "UPDATE Library SET IsRead=@r WHERE Id=@id AND UserId=@uid";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@r", chk.Checked);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
            }
            BindGrid("Okundu durumu güncellendi.");
        }
    }
}