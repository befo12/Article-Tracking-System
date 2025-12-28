using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using HtmlAgilityPack; // HtmlAgilityPack Yüklü Olmalı!

namespace WebApplication1.Pages
{
    public partial class Home : Page
    {
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;
        int CurrentUserId => Session["UserId"] == null ? 0 : (int)Session["UserId"];

        protected string Name { get; private set; }
        protected string Email { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0) { Response.Redirect("~/Pages/login.aspx"); return; }
            if (!IsPostBack)
            {
                LoadUserInfo();
                BindActivity();
                BindWeeklySuggestions();
            }
        }

        // === 1. KİŞİSELLEŞTİRİLMİŞ ÖNERİLER ===
        private void BindWeeklySuggestions()
        {
            int weekNum = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            string selectedTopic = "";

            List<string> userInterests = GetUserInterestTopics();
            if (userInterests.Count > 0)
            {
                selectedTopic = userInterests[weekNum % userInterests.Count];
                litTopic.Text = "Sizin İçin Seçildi: " + selectedTopic;
            }
            else
            {
                string[] defaultTopics = { "Artificial Intelligence", "Quantum Computing", "Cyber Security", "Bioinformatics", "Renewable Energy", "Data Science" };
                selectedTopic = defaultTopics[weekNum % defaultTopics.Length];
                litTopic.Text = "Haftanın Konusu: " + selectedTopic;
            }

            DataTable dt = FetchOpenAlexSimple(selectedTopic);
            rptWeekly.DataSource = dt;
            rptWeekly.DataBind();
        }

        // === 2. BUTONLA EKLEME (AKILLI İNDİRME) ===
        protected void rptWeekly_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "Add")
            {
                LinkButton btn = (LinkButton)e.CommandSource;
                string[] args = e.CommandArgument.ToString().Split(new[] { "||" }, StringSplitOptions.None);
                string title = args[0];
                string url = args.Length > 1 ? args[1] : "";

                if (!string.IsNullOrEmpty(url))
                {
                    UpsertLibrary(title, url, null); // Veritabanına yaz

                    // Butonu güncelle
                    btn.Text = "✔ Eklendi";
                    btn.CssClass = "btn btn-sm btn-success rounded-pill px-3 disabled";
                    btn.Enabled = false;

                    // Arka planda indirmeyi dene
                    string err;
                    TryFindAndDownloadBestCopy(null, url, title, CurrentUserId, out err);
                    BindActivity(); // Listeyi güncelle
                }
            }
        }

        // === 3. HIZLI EKLEME KUTUSU ===
        protected void btnQuickAdd_Click(object sender, EventArgs e)
        {
            string t = (txtQuickTitle.Text ?? "").Trim();
            string u = (txtQuickUrl.Text ?? "").Trim();
            string n = (txtQuickNote.Text ?? "").Trim(); // Hata buradaydı, artık düzeldi

            if (!string.IsNullOrEmpty(t) && !string.IsNullOrEmpty(u))
            {
                UpsertLibrary(t, u, n);

                txtQuickTitle.Text = ""; txtQuickUrl.Text = ""; txtQuickNote.Text = "";
                lblQuickMsg.Text = "Başarıyla eklendi!";
                lblQuickMsg.CssClass = "text-success fw-bold";

                // İndirmeyi dene
                TryFindAndDownloadBestCopy(null, u, t, CurrentUserId, out _);
                BindActivity(); // Listeyi yenile
            }
            else
            {
                lblQuickMsg.Text = "Başlık ve URL zorunludur.";
                lblQuickMsg.CssClass = "text-danger fw-bold";
            }
        }

        // === 4. VERİTABANI İŞLEMLERİ ===
        private int UpsertLibrary(string title, string url, string note)
        {
            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM Library WHERE UserId=@u AND Url=@url)
                BEGIN
                    INSERT INTO Library(UserId, Title, Url, Note, IsRead, AddedDate)
                    VALUES(@u, @t, @url, NULLIF(@n,''), 0, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE SELECT Id FROM Library WHERE UserId=@u AND Url=@url", con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                cmd.Parameters.AddWithValue("@t", title);
                cmd.Parameters.AddWithValue("@url", url);
                cmd.Parameters.AddWithValue("@n", (object)note ?? DBNull.Value);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void BindActivity()
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 5 Title, AddedDate AS [When], 
                'Kütüphaneye yeni bir makale eklendi' AS [Text]
                FROM Library WHERE UserId=@u ORDER BY AddedDate DESC", con))
            {
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                con.Open();
                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                rptFeed.DataSource = dt;
                rptFeed.DataBind();
            }
        }

        // === 5. İNDİRME MOTORU (SEARCH SAYFASINDAKİYLE AYNISI) ===
        private string TryFindAndDownloadBestCopy(string doi, string url, string title, int userId, out string error)
        {
            error = null;
            if (!string.IsNullOrWhiteSpace(url))
            {
                string s, m, e; bool p;
                if (DownloadToAppDataSmart(url, title, userId, true, out s, out m, out p, out e))
                {
                    string col = p ? "PdfPath" : "HtmlPath";
                    using (var c = new SqlConnection(Cs))
                    {
                        c.Open(); var cmd = new SqlCommand($"UPDATE Library SET {col}=@p WHERE UserId=@u AND Url=@url", c);
                        cmd.Parameters.AddWithValue("@p", s); cmd.Parameters.AddWithValue("@u", userId); cmd.Parameters.AddWithValue("@url", url);
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
                string folderAbs = Server.MapPath(folderRel);
                Directory.CreateDirectory(folderAbs);
                string cleanTitle = title;
                foreach (char c in Path.GetInvalidFileNameChars()) cleanTitle = cleanTitle.Replace(c, '_');
                if (cleanTitle.Length > 50) cleanTitle = cleanTitle.Substring(0, 50);
                string fName = cleanTitle + "_" + Guid.NewGuid().ToString().Substring(0, 4);

                var req = (HttpWebRequest)WebRequest.Create(fileUrl);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                req.Timeout = 25000; req.AllowAutoRedirect = true;

                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var rs = resp.GetResponseStream())
                {
                    byte[] head = new byte[8192];
                    int read = rs.Read(head, 0, head.Length);
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
                        // HTML ise kaydet ve tara
                        string h = Path.Combine(folderAbs, fName + ".html");
                        using (var fs = File.Create(h)) { if (read > 0) fs.Write(head, 0, read); rs.CopyTo(fs); }

                        try
                        { // HTML İÇİNDEN PDF ARAMA
                            string html = File.ReadAllText(h);
                            string baseUrl = resp.ResponseUri.ToString();
                            var doc = new HtmlDocument(); doc.LoadHtml(html);

                            var metaTags = new[] { "citation_pdf_url", "DC.identifier", "eprints.document_url" };
                            foreach (var tag in metaTags)
                            {
                                var node = doc.DocumentNode.SelectSingleNode($"//meta[@name='{tag}']");
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
                        }
                        catch { }

                        if (keepHtmlIfNoPdf) { savedRel = folderRel + fName + ".html"; return true; }
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; }
            return false;
        }

        // === DİĞER YARDIMCILAR ===
        private List<string> GetUserInterestTopics()
        {
            var l = new List<string>();
            try
            {
                using (var c = new SqlConnection(Cs))
                {
                    c.Open(); var cmd = new SqlCommand("SELECT Keyword FROM UserKeywords WHERE UserId=@u", c);
                    cmd.Parameters.AddWithValue("@u", CurrentUserId);
                    using (var r = cmd.ExecuteReader()) while (r.Read()) l.Add(r[0].ToString());
                }
            }
            catch { }
            return l;
        }

        private DataTable FetchOpenAlexSimple(string query)
        {
            DataTable t = new DataTable(); t.Columns.Add("Title"); t.Columns.Add("Authors"); t.Columns.Add("Venue"); t.Columns.Add("Year"); t.Columns.Add("Url");
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string url = "https://api.openalex.org/works?search=" + HttpUtility.UrlEncode(query) + "&per-page=6&sort=cited_by_count:desc&filter=from_publication_date:2022-01-01";
                string json = HttpGet(url);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                var root = ser.Deserialize<Dictionary<string, object>>(json);
                if (root != null && root.ContainsKey("results"))
                {
                    foreach (Dictionary<string, object> it in (ArrayList)root["results"])
                    {
                        string title = GetString(it, "display_name");
                        string year = GetString(it, "publication_year");
                        string doi = GetString(it, "doi").Replace("https://doi.org/", "");
                        string finalUrl = !string.IsNullOrEmpty(doi) ? "https://doi.org/" + doi : GetString(it, "id");
                        if (it.ContainsKey("open_access") && it["open_access"] is Dictionary<string, object> oa)
                            if (oa.ContainsKey("oa_url") && oa["oa_url"] != null) finalUrl = oa["oa_url"].ToString();
                        string author = "Yazar Yok";
                        if (it.ContainsKey("authorships") && it["authorships"] is ArrayList al && al.Count > 0)
                            if (((Dictionary<string, object>)al[0]).ContainsKey("author"))
                                author = GetString((Dictionary<string, object>)((Dictionary<string, object>)al[0])["author"], "display_name");
                        string venue = "";
                        if (it.ContainsKey("primary_location") && it["primary_location"] is Dictionary<string, object> loc)
                            if (loc.ContainsKey("source") && loc["source"] is Dictionary<string, object> src) venue = GetString(src, "display_name");
                        t.Rows.Add(title, author, venue, year, finalUrl);
                    }
                }
            }
            catch { }
            return t;
        }

        private void LoadUserInfo()
        {
            using (var c = new SqlConnection(Cs))
            {
                c.Open(); var cmd = new SqlCommand("SELECT Email, Name FROM Users WHERE Id=@id", c);
                cmd.Parameters.AddWithValue("@id", CurrentUserId);
                using (var r = cmd.ExecuteReader()) if (r.Read()) { Email = r["Email"].ToString(); Name = r["Name"].ToString(); if (string.IsNullOrEmpty(Name)) Name = Email; }
            }
        }
        private static string HttpGet(string url) { try { HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url); req.UserAgent = "HomeBot/1.0"; using (var r = req.GetResponse()) using (var s = new StreamReader(r.GetResponseStream())) return s.ReadToEnd(); } catch { return "{}"; } }
        private static string GetString(Dictionary<string, object> it, string key) => it.ContainsKey(key) && it[key] != null ? Convert.ToString(it[key]) : "";
    }
}