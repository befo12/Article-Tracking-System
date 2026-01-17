using HtmlAgilityPack; // HtmlAgilityPack Yüklü Olmalı!
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
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
            if (!IsPostBack) BindRealRecommendations();
        }

        private void BindRealRecommendations()
        {
            // 1. Kullanıcının kelimelerini çek
            List<string> tags = GetUserKeywords();
            if (tags.Count == 0) tags = new List<string> { "Artificial Intelligence", "Machine Learning", "Physics" };

            // İlk 3 konuyu al
            tags = tags.Take(4).ToList();
            var groups = new List<object>();

            foreach (var t in tags)
            {
                // CROSSREF'ten çek (Daha hızlı ve garanti)
                DataTable dt = FetchCrossrefForTopic(t, 4);
                if (dt.Rows.Count > 0)
                {
                    groups.Add(new { Tag = t, Items = dt });
                }
            }

            if (groups.Count > 0)
            {
                repGroups.DataSource = groups;
                repGroups.DataBind();
                lblInfo.Text = $"{groups.Count} konu başlığında Crossref sonuçları.";
                pnlNoData.Visible = false;
            }
            else
            {
                lblInfo.Text = "Sonuç yok.";
                pnlNoData.Visible = true;
            }
        }

        // --- İÇ REPEATER BAĞLAMA ---
        protected void repGroups_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                var dataItem = e.Item.DataItem;
                var prop = dataItem.GetType().GetProperty("Items");
                if (prop != null)
                {
                    DataTable dt = (DataTable)prop.GetValue(dataItem, null);
                    Repeater innerRep = (Repeater)e.Item.FindControl("repItems");
                    if (innerRep != null)
                    {
                        innerRep.DataSource = dt;
                        innerRep.DataBind();
                    }
                }
            }
        }

        // --- EKLE BUTONU VE İNDİRME ---
        protected void repItems_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "add")
            {
                var btn = (LinkButton)e.CommandSource;
                string[] args = e.CommandArgument.ToString().Split(new[] { "||" }, StringSplitOptions.None);
                string title = args[0];
                string url = args.Length > 1 ? args[1] : "";

                if (!string.IsNullOrEmpty(url))
                {
                    UpsertLibrary(title, url);

                    btn.Text = "✔ Eklendi";
                    btn.CssClass = "btn btn-sm btn-success disabled";
                    btn.Enabled = false;

                    // Arka planda indirmeyi dene
                    string err;
                    TryFindAndDownloadBestCopy(null, url, title, CurrentUserId, out err);
                }
            }
        }

        // --- CROSSREF API (Garanti Veri) ---
        private DataTable FetchCrossrefForTopic(string topic, int limit)
        {
            DataTable t = new DataTable();
            t.Columns.Add("Title"); t.Columns.Add("Summary"); t.Columns.Add("Url");

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                // Crossref'te 'relevance' (alaka düzeyi) ile arama yap
                string url = "https://api.crossref.org/works?query=" + HttpUtility.UrlEncode(topic) +
                             "&rows=" + limit + "&sort=relevance";

                string json = HttpGet(url);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                var root = ser.Deserialize<Dictionary<string, object>>(json);

                if (root != null && root.ContainsKey("message"))
                {
                    var msg = (Dictionary<string, object>)root["message"];
                    if (msg.ContainsKey("items"))
                    {
                        foreach (Dictionary<string, object> it in (ArrayList)msg["items"])
                        {
                            // Başlık
                            string title = "";
                            if (it.ContainsKey("title") && it["title"] is ArrayList tl && tl.Count > 0) title = tl[0].ToString();
                            if (string.IsNullOrEmpty(title)) continue;

                            // URL (Varsa Link, yoksa DOI)
                            string link = "";
                            if (it.ContainsKey("URL")) link = it["URL"].ToString();

                            // Yazar / Yıl
                            string year = "";
                            if (it.ContainsKey("published-print") && it["published-print"] is Dictionary<string, object> dp)
                                if (dp.ContainsKey("date-parts") && dp["date-parts"] is ArrayList dpa && dpa.Count > 0)
                                    year = ((ArrayList)dpa[0])[0].ToString();

                            string summary = string.IsNullOrEmpty(year) ? "Tarih Yok" : year;
                            t.Rows.Add(title, summary, link);
                        }
                    }
                }
            }
            catch { }
            return t;
        }

        // --- YARDIMCILAR (DB & DOWNLOADER) ---
        private List<string> GetUserKeywords()
        {
            var l = new List<string>();
            try { using (var c = new SqlConnection(Cs)) { c.Open(); var cmd = new SqlCommand("SELECT Keyword FROM UserKeywords WHERE UserId=@u", c); cmd.Parameters.AddWithValue("@u", CurrentUserId); using (var r = cmd.ExecuteReader()) while (r.Read()) l.Add(r[0].ToString()); } } catch { }
            return l;
        }

        private void UpsertLibrary(string title, string url)
        {
            using (var c = new SqlConnection(Cs))
            {
                c.Open(); var cmd = new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM Library WHERE UserId=@u AND Url=@l) INSERT INTO Library(UserId,Title,Url,AddedDate,IsRead) VALUES(@u,@t,@l,GETDATE(),0)", c);
                cmd.Parameters.AddWithValue("@u", CurrentUserId); cmd.Parameters.AddWithValue("@t", title); cmd.Parameters.AddWithValue("@l", url);
                cmd.ExecuteNonQuery();
            }
        }

        private static string HttpGet(string url) { try { HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url); r.UserAgent = "DashBot/1.0"; r.Timeout = 5000; using (var w = r.GetResponse()) using (var s = new StreamReader(w.GetResponseStream())) return s.ReadToEnd(); } catch { return "{}"; } }

        // --- TERMINATOR DOWNLOADER (Search.aspx ile aynı) ---
        private string TryFindAndDownloadBestCopy(string doi, string url, string title, int userId, out string error)
        {
            error = null;
            if (!string.IsNullOrEmpty(url))
            {
                string s, m, e; bool p;
                if (DownloadToAppDataSmart(url, title, userId, true, out s, out m, out p, out e))
                {
                    string col = p ? "PdfPath" : "HtmlPath";
                    using (var c = new SqlConnection(Cs))
                    {
                        c.Open(); var cmd = new SqlCommand($"UPDATE Library SET {col}=@p WHERE UserId=@u AND Url=@l", c);
                        cmd.Parameters.AddWithValue("@p", s); cmd.Parameters.AddWithValue("@u", userId); cmd.Parameters.AddWithValue("@l", url);
                        cmd.ExecuteNonQuery();
                    }
                    return s;
                }
            }
            return null;
        }

        private bool DownloadToAppDataSmart(string fileUrl, string title, int userId, bool keepHtmlIfNoPdf, out string savedRel, out string mime, out bool isPdf, out string error)
        {
            savedRel = null; mime = null; error = null; isPdf = false;
            try
            {
                if (fileUrl.StartsWith("http://")) fileUrl = fileUrl.Replace("http://", "https://");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string folderRel = "~/App_Data/Library/" + userId + "/";
                string folderAbs = Server.MapPath(folderRel); Directory.CreateDirectory(folderAbs);
                string fName = title.Substring(0, Math.Min(title.Length, 30)) + "_" + Guid.NewGuid().ToString().Substring(0, 4);
                foreach (char c in Path.GetInvalidFileNameChars()) fName = fName.Replace(c, '_');

                var req = (HttpWebRequest)WebRequest.Create(fileUrl);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                req.Timeout = 15000; req.AllowAutoRedirect = true;

                using (var resp = req.GetResponse()) using (var rs = resp.GetResponseStream())
                {
                    byte[] head = new byte[4096]; int read = rs.Read(head, 0, head.Length);
                    string hStr = Encoding.ASCII.GetString(head, 0, read);
                    bool looksPdf = hStr.Contains("%PDF-") || (resp.ContentType ?? "").Contains("pdf");

                    if (looksPdf)
                    {
                        string p = Path.Combine(folderAbs, fName + ".pdf");
                        using (var fs = File.Create(p)) { if (read > 0) fs.Write(head, 0, read); rs.CopyTo(fs); }
                        savedRel = folderRel + fName + ".pdf"; isPdf = true; return true;
                    }
                    else
                    {
                        string h = Path.Combine(folderAbs, fName + ".html");
                        using (var fs = File.Create(h)) { if (read > 0) fs.Write(head, 0, read); rs.CopyTo(fs); }
                        // Basit HTML tarama
                        try
                        {
                            string html = File.ReadAllText(h);
                            var doc = new HtmlDocument(); doc.LoadHtml(html);
                            var node = doc.DocumentNode.SelectSingleNode("//meta[@name='citation_pdf_url']");
                            if (node != null)
                            {
                                string pdfUrl = node.GetAttributeValue("content", "");
                                if (!string.IsNullOrEmpty(pdfUrl) && pdfUrl.StartsWith("http"))
                                {
                                    string s2, m2, e2; bool p2;
                                    if (DownloadToAppDataSmart(pdfUrl, title, userId, false, out s2, out m2, out p2, out e2))
                                    {
                                        if (p2) { savedRel = s2; isPdf = true; return true; }
                                    }
                                }
                            }
                        }
                        catch { }
                        if (keepHtmlIfNoPdf) { savedRel = folderRel + fName + ".html"; return true; }
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; }
            return false;
        }
    }
}