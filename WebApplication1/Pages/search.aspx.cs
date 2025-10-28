using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class Search : Page
    {
        int CurrentUserId => Session["UserId"] == null ? 0 : (int)Session["UserId"];
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0) { Response.Redirect(ResolveUrl("~/Pages/Login.aspx")); return; }

            if (!IsPostBack)
            {
                var q = Convert.ToString(Request["q"] ?? "").Trim();
                if (!string.IsNullOrEmpty(q))
                {
                    txtQuery.Text = q;
                    DoSearch(q);
                }
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            var q = txtQuery.Text.Trim();
            if (string.IsNullOrEmpty(q)) return;
            DoSearch(q);
        }

        private void DoSearch(string q)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string url = "https://api.crossref.org/works?rows=20&query=" + HttpUtility.UrlEncode(q);
                var json = HttpGet(url);
                var table = ParseCrossref(json);

                if (!table.Columns.Contains("InLibrary"))
                    table.Columns.Add("InLibrary", typeof(bool));

                foreach (DataRow r in table.Rows)
                {
                    string u = Convert.ToString(r["Url"]);
                    r["InLibrary"] = IsInLibrary(u);
                }

                gvResults.DataSource = table;
                gvResults.DataBind();
                lblInfo.Text = table.Rows.Count + " sonuç";
            }
            catch (Exception ex)
            {
                lblInfo.Text = "Hata: " + ex.Message;
            }
        }

        private bool IsInLibrary(string url)
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand("SELECT 1 FROM Library WHERE UserId=@u AND Url=@url", con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                cmd.Parameters.AddWithValue("@url", (object)url ?? DBNull.Value);
                con.Open();
                var v = cmd.ExecuteScalar();
                return v != null;
            }
        }

        protected void gvResults_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            if (e.Row.RowType != System.Web.UI.WebControls.DataControlRowType.DataRow) return;
            bool inLib = false;
            var key = gvResults.DataKeys[e.Row.RowIndex];
            if (key != null)
            {
                var val = key.Values["InLibrary"];
                inLib = val != null && Convert.ToBoolean(val);
            }
            var btnAdd = (System.Web.UI.WebControls.LinkButton)e.Row.FindControl("btnAdd");
            var btnRemove = (System.Web.UI.WebControls.LinkButton)e.Row.FindControl("btnRemove");
            if (btnAdd != null) btnAdd.Visible = !inLib;
            if (btnRemove != null) btnRemove.Visible = inLib;
        }

        protected void gvResults_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName != "AddToLib" && e.CommandName != "RemoveFromLib") return;

            int rowIndex = Convert.ToInt32(e.CommandArgument);
            var key = gvResults.DataKeys[rowIndex];
            string url = key.Values["Url"]?.ToString() ?? "";
            string title = key.Values["Title"]?.ToString() ?? "(başlıksız)";

            if (e.CommandName == "AddToLib")
            {
                UpsertLibrary(CurrentUserId, title, url);
                lblInfo.Text = "Kütüphaneye eklendi: " + Server.HtmlEncode(title);
            }
            else if (e.CommandName == "RemoveFromLib")
            {
                RemoveFromLibrary(CurrentUserId, url);
                lblInfo.Text = "Kütüphaneden çıkarıldı: " + Server.HtmlEncode(title);
            }

            // Listeyi tazele
            btnSearch_Click(null, EventArgs.Empty);
        }

        private int UpsertLibrary(int userId, string title, string url)
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM Library WHERE UserId=@uid AND Url=@u)
    SELECT Id FROM Library WHERE UserId=@uid AND Url=@u;
ELSE
BEGIN
    INSERT INTO Library(UserId, Title, Url, Note, IsRead)
    VALUES(@uid, @t, @u, NULL, 0);
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

        private void RemoveFromLibrary(int userId, string url)
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand("DELETE FROM Library WHERE UserId=@uid AND Url=@u", con))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@u", (object)url ?? DBNull.Value);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // --- yardımcılar ---
        private static string HttpGet(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "WebFormsDemo/1.0";
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static DataTable ParseCrossref(string json)
        {
            var ser = new JavaScriptSerializer();
            var root = ser.Deserialize<Dictionary<string, object>>(json);
            var msg = (Dictionary<string, object>)root["message"];
            var items = (ArrayList)msg["items"];

            var t = new DataTable();
            t.Columns.Add("Title");
            t.Columns.Add("Authors");
            t.Columns.Add("Venue");
            t.Columns.Add("Year");
            t.Columns.Add("Url");

            foreach (Dictionary<string, object> it in items)
            {
                string title = First(it, "title");
                string venue = First(it, "container-title");
                string url = GetString(it, "URL");
                string authors = JoinAuthors(it);
                string year = GetYear(it);
                t.Rows.Add(title, authors, venue, year, url);
            }
            return t;
        }

        private static string First(Dictionary<string, object> it, string key)
        {
            if (!it.ContainsKey(key) || it[key] == null) return "";
            var arr = it[key] as ArrayList;
            if (arr != null && arr.Count > 0) return Convert.ToString(arr[0]);
            return Convert.ToString(it[key]);
        }
        private static string GetString(Dictionary<string, object> it, string key)
            => it.ContainsKey(key) && it[key] != null ? Convert.ToString(it[key]) : "";
        private static string JoinAuthors(Dictionary<string, object> it)
        {
            if (!it.ContainsKey("author") || it["author"] == null) return "";
            var arr = it["author"] as ArrayList; if (arr == null) return "";
            var names = new List<string>();
            foreach (Dictionary<string, object> a in arr)
            {
                string given = a.ContainsKey("given") ? Convert.ToString(a["given"]) : "";
                string family = a.ContainsKey("family") ? Convert.ToString(a["family"]) : "";
                string full = (given + " " + family).Trim();
                if (!string.IsNullOrEmpty(full)) names.Add(full);
            }
            return string.Join(", ", names.ToArray());
        }
        private static string GetYear(Dictionary<string, object> it)
        {
            foreach (var key in new[] { "published-print", "published-online", "issued" })
            {
                if (!it.ContainsKey(key) || it[key] == null) continue;
                var pub = it[key] as Dictionary<string, object>;
                if (pub == null || !pub.ContainsKey("date-parts")) continue;
                var outer = pub["date-parts"] as ArrayList; if (outer == null || outer.Count == 0) continue;
                var inner = outer[0] as ArrayList; if (inner == null || inner.Count == 0) continue;
                return Convert.ToString(inner[0]);
            }
            return "";
        }
    }
}
