using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
// eklenenler:
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
            if (CurrentUserId == 0)
            {
                Response.Redirect("~/Pages/Login.aspx");
                return;
            }
            if (!IsPostBack)
                BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            gvLib.PageIndex = 0;
            BindGrid();
        }

        protected void gvLib_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvLib.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        private void BindGrid()
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = con;

                // temel koşul
                var where = "WHERE UserId=@uid";
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);

                // okundu filtresi
                bool? r = null;
                if (!string.IsNullOrEmpty(ddlRead.SelectedValue))
                    r = ddlRead.SelectedValue == "1";
                if (r.HasValue)
                {
                    where += " AND IsRead=@r";
                    cmd.Parameters.Add("@r", SqlDbType.Bit).Value = r.Value;
                }

                // anahtar kelimeler
                string raw = (txtSearch.Text ?? "").Trim();
                var tokens = Regex.Split(raw, @"\s+")
                                  .Where(t => !string.IsNullOrWhiteSpace(t))
                                  .ToList();

                if (tokens.Count > 0)
                {
                    string scope = ddlSearchIn.SelectedValue; // title | note | both
                    string match = ddlMatch.SelectedValue;    // AND | OR

                    var groupClauses = new List<string>();
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        string p = "@t" + i;
                        cmd.Parameters.AddWithValue(p, "%" + tokens[i] + "%");

                        string fieldExpr = scope == "title"
                            ? $"Title LIKE {p}"
                            : scope == "note"
                                ? $"ISNULL(Note,'') LIKE {p}"
                                : $"(Title LIKE {p} OR ISNULL(Note,'') LIKE {p})";

                        groupClauses.Add(fieldExpr);
                    }

                    string glue = match == "OR" ? " OR " : " AND ";
                    where += " AND (" + string.Join(glue, groupClauses) + ")";
                }

                cmd.CommandText = @"
SELECT Id, UserId, Title, Url, Note, IsRead, AddedDate
FROM Library
" + where + @"
ORDER BY AddedDate DESC, Id DESC;";

                var dt = new DataTable();
                con.Open();
                dt.Load(cmd.ExecuteReader());

                gvLib.DataSource = dt;
                gvLib.DataBind();

                lblInfo.Text = $"{dt.Rows.Count} kayıt listelendi.";
            }
        }

        protected void gvLib_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "remove")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                using (var con = new SqlConnection(Cs))
                using (var cmd = new SqlCommand("DELETE FROM Library WHERE Id=@id AND UserId=@uid", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                lblInfo.Text = "Kayıt silindi.";
                BindGrid();
            }
            else if (e.CommandName == "saveNote")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                var link = (LinkButton)e.CommandSource;
                var row = (GridViewRow)link.NamingContainer;
                var txtNote = (TextBox)row.FindControl("txtNote");
                var note = (txtNote?.Text ?? "").Trim();

                using (var con = new SqlConnection(Cs))
                using (var cmd = new SqlCommand(
                    "UPDATE Library SET Note=@n WHERE Id=@id AND UserId=@uid", con))
                {
                    cmd.Parameters.AddWithValue("@n", (object)note ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                lblInfo.Text = "Not kaydedildi.";
                BindGrid();
            }
        }

        protected void ChkReadChanged(object sender, EventArgs e)
        {
            var chk = (CheckBox)sender;
            var row = (GridViewRow)chk.NamingContainer;
            int id = Convert.ToInt32(gvLib.DataKeys[row.RowIndex].Value);

            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(
                "UPDATE Library SET IsRead=@r WHERE Id=@id AND UserId=@uid", con))
            {
                cmd.Parameters.AddWithValue("@r", chk.Checked);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            lblInfo.Text = chk.Checked ? "Okundu olarak işaretlendi." : "Okunmadı olarak işaretlendi.";
            BindGrid();
        }
    }
}
