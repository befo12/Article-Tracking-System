using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebApplication1.Pages
{
    public partial class Dashboard : Page
    {
        int CurrentUserId => Session["UserId"] == null ? 0 : Convert.ToInt32(Session["UserId"]);
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0) { Response.Redirect("~/Pages/Login.aspx"); return; }
            if (!IsPostBack) BindRecommendations();
        }

        // --- Kişiselleştirilmiş öneriler ---
        private void BindRecommendations()
        {
            // 1) DB'den anahtar kelimeleri UserKeywords tablosundan oku
            var kwFromDb = GetKeywordsFromUserKeywords(CurrentUserId);

            // 2) Yoksa Session veya varsayılan
            string kws = kwFromDb ?? Convert.ToString(Session["keywords"] ?? "AI; ML; Physics");
            var tags = SplitTags(kws);

            var groups = new List<object>();
            int id = 1;
            foreach (var t in tags)
            {
                var items = new List<object>
                {
                    new { Id = id++, Title = $"{t} – Yeni yöntemler",  Summary = "Kısa özet yer alır.", Url = "https://example.com/" + Uri.EscapeDataString(t) },
                    new { Id = id++, Title = $"{t} – Son çalışmalar", Summary = "Kısa özet yer alır.", Url = "https://example.com/" + Uri.EscapeDataString(t) + "2" }
                };
                groups.Add(new { Tag = t, Items = items });
            }

            repGroups.DataSource = groups;
            repGroups.DataBind();

            // İç repeater'ları tek tek bağla
            for (int i = 0; i < repGroups.Items.Count; i++)
            {
                var outerItem = repGroups.Items[i];
                var repItems = (Repeater)outerItem.FindControl("repItems");
                var group = (dynamic)groups[i];
                repItems.DataSource = group.Items;
                repItems.DataBind();
            }

            lblInfo.Text = $"{tags.Count} konu için öneriler listelendi.";
        }

        // UserKeywords tablosundan ; ile birleştir
        private string GetKeywordsFromUserKeywords(int userId)
        {
            try
            {
                using (var con = new SqlConnection(Cs))
                using (var cmd = new SqlCommand(
                    // SQL Server 2017+ ise STRING_AGG
                    "SELECT STRING_AGG(Keyword, '; ') WITHIN GROUP (ORDER BY Keyword) " +
                    "FROM UserKeywords WHERE UserId=@u;", con))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    con.Open();
                    var v = cmd.ExecuteScalar();
                    return v == null || v == DBNull.Value ? null : Convert.ToString(v);
                }
            }
            catch
            {
                // 2016 ve öncesi için fallback (FOR XML PATH)
                using (var con = new SqlConnection(Cs))
                using (var cmd = new SqlCommand(@"
DECLARE @s NVARCHAR(MAX)='';
SELECT @s = @s + CASE WHEN LEN(@s)>0 THEN '; ' ELSE '' END + Keyword
FROM UserKeywords WHERE UserId=@u ORDER BY Keyword;
SELECT @s;", con))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    con.Open();
                    var v = cmd.ExecuteScalar();
                    return v == null || v == DBNull.Value ? null : Convert.ToString(v);
                }
            }
        }

        private static List<string> SplitTags(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(s)) return list;
            var parts = s.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var v = p.Trim();
                if (v.Length > 0 && !list.Exists(x => x.Equals(v, StringComparison.OrdinalIgnoreCase)))
                    list.Add(v);
            }
            return list;
        }

        // --- İç repeater komutunu yakala ve Library'ye yaz ---
        protected void RepItems_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "add") return;

            var arg = Convert.ToString(e.CommandArgument ?? "");
            // CommandArgument formatı: Title||Url
            string title = "", url = "";
            var parts = arg.Split(new[] { "||" }, StringSplitOptions.None);
            if (parts.Length >= 2) { title = parts[0]; url = parts[1]; }

            if (string.IsNullOrWhiteSpace(url))
            {
                lblInfo.Text = "Geçersiz bağlantı.";
                return;
            }

            int libId = UpsertLibrary(CurrentUserId, title, url);
            lblInfo.Text = libId > 0
                ? $"Kütüphaneye eklendi: {Server.HtmlEncode(title)}"
                : "Zaten kütüphanende.";
        }

        private int UpsertLibrary(int userId, string title, string url)
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM Library WHERE UserId=@uid AND Url=@u)
    SELECT CAST((SELECT TOP 1 Id FROM Library WHERE UserId=@uid AND Url=@u) AS INT);
ELSE
BEGIN
    INSERT INTO Library(UserId, Title, Url, Note, IsRead, AddedDate)
    VALUES(@uid, @t, @u, NULL, 0, GETDATE());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END", con))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@t", (object)title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@u", (object)url ?? DBNull.Value);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
