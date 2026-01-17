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
using HtmlAgilityPack; // NuGet'ten HtmlAgilityPack YÜKLÜ OLMALI!

namespace WebApplication1.Pages
{
    public partial class Search : Page
    {
        int CurrentUserId => Session["UserId"] == null ? 0 : (int)Session["UserId"];
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        DataTable BaseTable
        {
            get => ViewState["BaseTable"] as DataTable;
            set => ViewState["BaseTable"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserId == 0)
            {
                Response.Redirect(ResolveUrl("~/Pages/Login.aspx"));
                return;
            }
        }

        // === ARAMA İŞLEMLERİ ===
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string q = (txtQuery.Text ?? "").Trim();
            if (q.Length == 0) return;
            // search.aspx.cs içine eklenecek mantık
            // btnSearch_Click içindeki veritabanı kayıt kısmı
            using (SqlConnection con = new SqlConnection(Cs))
            {
                // Veritabanı tablonuzdaki sütun adı 'SearchTerm' olduğu için burayı düzelttik
                string sql = "INSERT INTO SearchHistory (UserId, SearchTerm, SearchDate) VALUES (@u, @q, GETDATE())";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@u", CurrentUserId);
                cmd.Parameters.AddWithValue("@q", txtQuery.Text);

                con.Open();
                cmd.ExecuteNonQuery(); // Artık hata vermeyecek
            }

            lblInfo.Text = "Aranıyor...";

            var t1 = FetchCrossrefBatch(q, 25, "*", out string nextCursor);
            hfCursor.Value = nextCursor ?? "";
            var t2 = FetchOpenAlexBatch(q);

            t1.Merge(t2);

            DataTable distinctTable = t1.Clone();
            if (t1.Rows.Count > 0)
            {
                var uniqueRows = t1.AsEnumerable()
                    .GroupBy(r =>
                    {
                        string doi = r["Doi"]?.ToString() ?? "";
                        string title = r["Title"]?.ToString().ToLowerInvariant() ?? "";
                        return string.IsNullOrEmpty(doi) ? title : doi;
                    })
                    .Select(g => g.OrderByDescending(r => r.Field<bool>("HasPdf") == true).First());

                if (uniqueRows.Any()) distinctTable = uniqueRows.CopyToDataTable();
            }

            EnsureColumns(distinctTable);
            foreach (DataRow r in distinctTable.Rows)
                r["InLibrary"] = IsInLibrary(Convert.ToString(r["Url"]));

            BaseTable = distinctTable;
            gvResults.PageIndex = 0;
            ApplyFiltersAndBind();
            lblInfo.Text = $"{distinctTable.Rows.Count} sonuç bulundu.";
        }

        protected void btnLoadMore_Click(object sender, EventArgs e)
        {
            if (BaseTable == null) return;
            string q = (txtQuery.Text ?? "").Trim();
            string cursor = hfCursor.Value;
            if (q.Length == 0 || string.IsNullOrEmpty(cursor)) return;

            var more = FetchCrossrefBatch(q, 50, cursor, out string nextCursor);
            hfCursor.Value = nextCursor ?? "";
            EnsureColumns(more);
            foreach (DataRow r in more.Rows) r["InLibrary"] = IsInLibrary(Convert.ToString(r["Url"]));

            var all = BaseTable;
            var keys = new HashSet<string>(all.AsEnumerable().Select(r => ((r["Doi"] + "|" + r["Url"]) ?? "").ToString().ToLowerInvariant()));
            foreach (DataRow add in more.Rows)
            {
                string k = ((add["Doi"] + "|" + add["Url"]) ?? "").ToString().ToLowerInvariant();
                if (!keys.Contains(k)) { all.ImportRow(add); keys.Add(k); }
            }
            BaseTable = all;
            ApplyFiltersAndBind();
            lblInfo.Text = $"Toplam {all.Rows.Count} sonuç.";
        }

        // === GRID EVENTS ===
        protected void gvResults_PageIndexChanging(object sender, GridViewPageEventArgs e) { gvResults.PageIndex = e.NewPageIndex; ApplyFiltersAndBind(); }
        protected void gvResults_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            bool inLib = false;
            var key = gvResults.DataKeys[e.Row.RowIndex];
            if (key != null && key.Values["InLibrary"] != null) inLib = Convert.ToBoolean(key.Values["InLibrary"]);
            if (inLib) e.Row.CssClass += " result-row inlib";
            var btnAdd = (LinkButton)e.Row.FindControl("btnAdd");
            var btnRemove = (LinkButton)e.Row.FindControl("btnRemove");
            if (btnAdd != null) btnAdd.Visible = !inLib;
            if (btnRemove != null) btnRemove.Visible = inLib;
        }
        protected void gvResults_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "AddToLib" && e.CommandName != "RemoveFromLib") return;
            int idx = Convert.ToInt32(e.CommandArgument);
            DataKey key = gvResults.DataKeys[idx];
            string url = Convert.ToString(key.Values["Url"]);
            string title = Convert.ToString(key.Values["Title"]);
            string doi = Convert.ToString(key.Values["Doi"]);
            bool inLib = Convert.ToBoolean(key.Values["InLibrary"]);

            if (e.CommandName == "AddToLib" && !inLib)
            {
                UpsertLibrary(CurrentUserId, title, url, doi);
                inLib = true;
                lblInfo.Text = "Kütüphaneye eklendi: " + Server.HtmlEncode(title);
            }
            else if (e.CommandName == "RemoveFromLib" && inLib)
            {
                RemoveFromLibrary(CurrentUserId, url);
                inLib = false;
                lblInfo.Text = "Çıkarıldı.";
            }
            var t = BaseTable;
            var row = t?.AsEnumerable().FirstOrDefault(r => Convert.ToString(r["Url"]) == url);
            if (row != null) row["InLibrary"] = inLib;
            ApplyFiltersAndBind();
        }
        protected void FilterChanged(object sender, EventArgs e) => ApplyFiltersAndBind();
        private void ApplyFiltersAndBind()
        {
            var table = BaseTable;
            if (table == null) { gvResults.DataSource = null; gvResults.DataBind(); return; }
            IEnumerable<DataRow> q = table.AsEnumerable();

            int yFrom = ParseInt(txtYearFrom.Text);
            int yTo = ParseInt(txtYearTo.Text);
            if (yFrom > 0) q = q.Where(r => ParseInt(r["Year"]?.ToString()) >= yFrom);
            if (yTo > 0) q = q.Where(r => ParseInt(r["Year"]?.ToString()) <= yTo);

            string author = (txtAuthor.Text ?? "").Trim();
            if (author.Length > 0) q = q.Where(r => ContainsCI(r["Authors"]?.ToString(), author));
            string venue = (txtVenue.Text ?? "").Trim();
            if (venue.Length > 0) q = q.Where(r => ContainsCI(r["Venue"]?.ToString(), venue));
            string lang = (ddlLang.SelectedValue ?? "").Trim();
            if (lang.Length > 0) q = q.Where(r => (r["Language"]?.ToString() ?? "").Equals(lang, StringComparison.OrdinalIgnoreCase));
            string exact = (txtExact.Text ?? "").Trim().Trim('"');
            if (chkExactPhrase.Checked && exact.Length > 0) q = q.Where(r => ContainsCI(r["Title"]?.ToString(), exact));
            if (chkHasPdf.Checked && table.Columns.Contains("HasPdf")) q = q.Where(r => r.Field<bool>("HasPdf") == true);
            if (chkInLibOnly.Checked) q = q.Where(r => r.Field<bool>("InLibrary") == true);

            switch (ddlSort.SelectedValue ?? "relevance")
            {
                case "date_desc": q = q.OrderByDescending(r => ParseInt(r["Year"]?.ToString())).ThenBy(r => r["Title"]); break;
                case "date_asc": q = q.OrderBy(r => ParseInt(r["Year"]?.ToString())).ThenBy(r => r["Title"]); break;
                case "title_asc": q = q.OrderBy(r => r["Title"]); break;
                default: break;
            }
            var view = q.Any() ? q.CopyToDataTable() : table.Clone();
            gvResults.DataSource = view; gvResults.DataBind();
            lblInfo.Text = $"{view.Rows.Count} sonuç listeleniyor.";
        }

        // === KAYDETME VE İNDİRME ===
        private int UpsertLibrary(int userId, string title, string url, string doi)
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM Library WHERE UserId=@uid AND Url=@u)
            BEGIN
              UPDATE Library SET Title=ISNULL(@t, Title), Doi=ISNULL(@d, Doi)
              WHERE UserId=@uid AND Url=@u;
              SELECT Id FROM Library WHERE UserId=@uid AND Url=@u;
            END
            ELSE
            BEGIN
              INSERT INTO Library(UserId, Title, Url, Doi, Note, IsRead)
              VALUES(@uid, @t, @u, @d, NULL, 0);
              SELECT CAST(SCOPE_IDENTITY() AS INT);
            END", con))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@t", (object)title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@u", (object)url ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d", string.IsNullOrWhiteSpace(doi) ? (object)DBNull.Value : doi);

                con.Open();
                int id = Convert.ToInt32(cmd.ExecuteScalar());
                con.Close();

                string err;
                string savedRel = TryFindAndDownloadBestCopy(doi, url, title, userId, out err);

                if (!string.IsNullOrEmpty(savedRel))
                {
                    string ext = Path.GetExtension(savedRel).ToLowerInvariant();
                    string sql = (ext == ".html" || ext == ".htm") ? "UPDATE Library SET HtmlPath=@p WHERE Id=@id" : "UPDATE Library SET PdfPath=@p WHERE Id=@id";
                    using (var con2 = new SqlConnection(Cs))
                    using (var cmd2 = new SqlCommand(sql, con2))
                    {
                        cmd2.Parameters.AddWithValue("@p", savedRel);
                        cmd2.Parameters.AddWithValue("@id", id);
                        con2.Open();
                        cmd2.ExecuteNonQuery();
                    }
                }
                return id;
            }
        }

        private void RemoveFromLibrary(int userId, string url)
        {
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand("DELETE FROM Library WHERE UserId=@uid AND Url=@u", con))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@u", (object)url ?? DBNull.Value);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // === STRATEJİK İNDİRME MOTORU ===
        private string TryFindAndDownloadBestCopy(string doi, string url, string title, int userId, out string error)
        {
            error = null;
            string nDoi = NormalizeDoi(doi);
            string firstHtmlRel = null;

            // 1. ADIM: HIZLI VE KESİN (Direkt Link)
            // Eğer URL .pdf ise hemen saldır
            if (!string.IsNullOrWhiteSpace(url) && (url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || url.Contains("/pdf/")))
            {
                string s, m; bool p; string e;
                // keepHtmlIfNoPdf=false (Direkt PDF bekliyoruz)
                if (DownloadToAppDataSmart(url, title, userId, false, out s, out m, out p, out e))
                {
                    if (p) return s; // Bulduk!
                }
            }

            // 2. ADIM: DOI TARAMA (Akademik Kapı)
            try
            {
                string s1, m1, e1; bool p1;
                // DOI'den indir, PDF gelmezse HTML'i al ve içini tara
                if (TryDoiThenMaybeHtml_Inner(nDoi, title, userId, out s1, out m1, out p1, out e1))
                {
                    if (p1) return s1; // PDF geldi
                    if (firstHtmlRel == null) firstHtmlRel = s1; // HTML geldi, cepte dursun
                }
            }
            catch { }

            // 3. ADIM: ORİJİNAL URL TARAMA (Fallback)
            try
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    string s2, m2, e2; bool p2;
                    // Orijinal URL'yi indir, PDF değilse içindeki linkleri tara
                    if (DownloadToAppDataSmart(url, title, userId, true, out s2, out m2, out p2, out e2))
                    {
                        if (p2) return s2; // PDF geldi
                        if (firstHtmlRel == null) firstHtmlRel = s2;
                    }
                }
            }
            catch { }

            // Hiç PDF bulamazsak elimizdeki en iyi HTML'i (web sayfası kopyasını) verelim
            return firstHtmlRel;
        }

        private bool TryDoiThenMaybeHtml_Inner(string normalizedDoi, string title, int userId, out string savedRel, out string mime, out bool isPdf, out string error)
        {
            savedRel = null; mime = null; isPdf = false; error = null;
            if (string.IsNullOrWhiteSpace(normalizedDoi)) return false;
            string doiUrl = "https://doi.org/" + normalizedDoi;
            return DownloadToAppDataSmart(doiUrl, title, userId, true, out savedRel, out mime, out isPdf, out error);
        }

        // === GELİŞMİŞ İNDİRME VE TARAMA ÇEKİRDEĞİ ===
        private bool DownloadToAppDataSmart(string fileUrl, string title, int userId, bool keepHtmlIfNoPdf, out string savedRel, out string mime, out bool isPdf, out string error)
        {
            savedRel = null; mime = null; error = null; isPdf = false;
            try
            {
                // 1. HTTP -> HTTPS DÜZELTMESİ (Hindawi gibi siteler için kritik!)
                if (fileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    fileUrl = fileUrl.Replace("http://", "https://");

                // Tls13 kısmını sildik, Tls12 yeterlidir.
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; string folderRel = "~/App_Data/Library/" + userId + "/";
                string folderAbs = Server.MapPath(folderRel);
                Directory.CreateDirectory(folderAbs);

                foreach (char c in Path.GetInvalidFileNameChars()) title = title.Replace(c, '_');
                if (title.Length > 50) title = title.Substring(0, 50);
                string suggestedName = title + "_" + Guid.NewGuid().ToString().Substring(0, 4);

                var req = (HttpWebRequest)WebRequest.Create(fileUrl);
                req.Method = "GET";
                // 2. KİMLİK GİZLEME (Chrome taklidi - 403 hatalarını önler)
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Timeout = 25000; // 25 sn zaman aşımı
                req.AllowAutoRedirect = true;

                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var rs = resp.GetResponseStream())
                {
                    string ct = (resp.ContentType ?? "").ToLowerInvariant();
                    byte[] head = new byte[8192];
                    int read = rs.Read(head, 0, head.Length);

                    // 3. İÇERİK KONTROLÜ (PDF mi HTML mi?)
                    bool looksPdf = false;
                    string headerStr = Encoding.ASCII.GetString(head, 0, read);

                    // PDF imzası dosyanın başında olmak zorunda değil, ilk 8KB içinde arayalım
                    if (headerStr.Contains("%PDF-")) looksPdf = true;
                    else if (ct.Contains("pdf")) looksPdf = true;

                    if (looksPdf)
                    {
                        // PDF BULUNDU! KAYDET.
                        string absPdf = Path.Combine(folderAbs, suggestedName + ".pdf");
                        using (FileStream fs = File.Create(absPdf))
                        {
                            if (read > 0) fs.Write(head, 0, read);
                            rs.CopyTo(fs);
                        }
                        savedRel = folderRel + suggestedName + ".pdf";
                        mime = "application/pdf";
                        isPdf = true;
                        return true;
                    }
                    else
                    {
                        // HTML GELDİ. KAYDET VE İÇİNDE PDF ARA.
                        string absHtml = Path.Combine(folderAbs, suggestedName + ".html");
                        using (FileStream fs = File.Create(absHtml))
                        {
                            if (read > 0) fs.Write(head, 0, read);
                            rs.CopyTo(fs);
                        }

                        // === HTML SCRAPING (AVCI) ===
                        try
                        {
                            string htmlContent = File.ReadAllText(absHtml);
                            string baseUrl = resp.ResponseUri.ToString(); // Yönlendirilmiş son adres

                            List<string> candidates = ExtractPdfCandidatesByHAP(htmlContent, baseUrl);

                            foreach (var cand in candidates)
                            {
                                // Kendini tekrar çağırmasın
                                if (cand == fileUrl) continue;

                                string s2, m2, e2; bool p2;
                                // Recursive: Bulduğun linki indir, PDF mi diye bak
                                if (DownloadToAppDataSmart(cand, title, userId, false, out s2, out m2, out p2, out e2))
                                {
                                    if (p2) // Bingo!
                                    {
                                        try { File.Delete(absHtml); } catch { } // HTML'i çöpe at
                                        savedRel = s2; mime = m2; isPdf = true;
                                        return true;
                                    }
                                }
                            }
                        }
                        catch { }

                        // PDF bulamazsak HTML'i döndür
                        if (keepHtmlIfNoPdf)
                        {
                            savedRel = folderRel + suggestedName + ".html";
                            mime = "text/html";
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; }
            return false;
        }

        // === HTML İÇİNDEN PDF LİNKİ ÇIKARMA ===
        private List<string> ExtractPdfCandidatesByHAP(string html, string baseUrl)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(html)) return list;
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 1. META TAGLER (En Temiz Kaynak)
                var metaTags = new[] { "citation_pdf_url", "DC.identifier", "eprints.document_url", "bepress_citation_pdf_url" };
                foreach (var tag in metaTags)
                {
                    var node = doc.DocumentNode.SelectSingleNode($"//meta[@name='{tag}']");
                    if (node != null)
                    {
                        string c = node.GetAttributeValue("content", "");
                        if (!string.IsNullOrEmpty(c) && c.StartsWith("http")) list.Add(c);
                    }
                }

                // 2. LİNKLERİ TARA
                var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        string href = n.GetAttributeValue("href", "");
                        string text = n.InnerText.ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#") || href.StartsWith("javascript")) continue;

                        string abs = href;
                        try { abs = new Uri(new Uri(baseUrl), href).ToString(); } catch { }

                        // PDF kokusu alan linkleri topla
                        if (abs.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) list.Add(abs);
                        else if (abs.Contains("/pdf/") || abs.Contains("article-pdf")) list.Add(abs);
                        else if (text.Contains("pdf") || text.Contains("download full")) list.Add(abs);
                    }
                }
            }
            catch { }
            return list.Distinct().ToList();
        }

        // === API YARDIMCILARI ===
        private DataTable FetchCrossrefBatch(string query, int pageSize, string cursor, out string nextCursor) { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; string url = "https://api.crossref.org/works?rows=" + pageSize + "&query=" + HttpUtility.UrlEncode(query) + "&cursor=" + HttpUtility.UrlEncode(cursor); string json = HttpGet(url); var ser = new JavaScriptSerializer(); var root = ser.Deserialize<Dictionary<string, object>>(json); nextCursor = null; if (root != null && root.ContainsKey("message")) { var msg = (Dictionary<string, object>)root["message"]; nextCursor = msg.ContainsKey("next-cursor") ? Convert.ToString(msg["next-cursor"]) : null; } return ParseCrossref(json); }
        private DataTable FetchOpenAlexBatch(string query) { try { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; string url = "https://api.openalex.org/works?search=" + HttpUtility.UrlEncode(query) + "&per-page=25"; string json = HttpGet(url); JavaScriptSerializer ser = new JavaScriptSerializer(); Dictionary<string, object> root = ser.Deserialize<Dictionary<string, object>>(json); if (root == null || !root.ContainsKey("results")) return CreateEmptyTable(); ArrayList items = (ArrayList)root["results"]; DataTable t = CreateEmptyTable(); foreach (Dictionary<string, object> it in items) { string title = GetString(it, "display_name"); string authors = ""; if (it.ContainsKey("authorships") && it["authorships"] is ArrayList authList) { var temp = new List<string>(); foreach (Dictionary<string, object> auEntry in authList) { if (auEntry.ContainsKey("author") && auEntry["author"] is Dictionary<string, object> auObj) temp.Add(GetString(auObj, "display_name")); } authors = string.Join(", ", temp.Take(3)); } string venue = ""; if (it.ContainsKey("primary_location") && it["primary_location"] is Dictionary<string, object> loc) { if (loc != null && loc.ContainsKey("source") && loc["source"] is Dictionary<string, object> src) venue = GetString(src, "display_name"); } string year = GetString(it, "publication_year"); string doi = GetString(it, "doi").Replace("https://doi.org/", "").Trim(); string idUrl = GetString(it, "id"); bool hasPdf = false; string pdfUrl = ""; if (it.ContainsKey("open_access") && it["open_access"] is Dictionary<string, object> oa) { if (oa.ContainsKey("is_oa") && (bool)oa["is_oa"] == true) { hasPdf = true; pdfUrl = GetString(oa, "oa_url"); } } string finalUrl = !string.IsNullOrEmpty(pdfUrl) ? pdfUrl : (!string.IsNullOrEmpty(doi) ? "https://doi.org/" + doi : idUrl); string lang = ""; t.Rows.Add(title, authors, venue, year, finalUrl, doi, hasPdf, lang, false); } return t; } catch { return CreateEmptyTable(); } }
        private static DataTable ParseCrossref(string json) { JavaScriptSerializer ser = new JavaScriptSerializer(); Dictionary<string, object> root = ser.Deserialize<Dictionary<string, object>>(json); DataTable t = CreateEmptyTable(); if (root == null || !root.ContainsKey("message")) return t; Dictionary<string, object> msg = (Dictionary<string, object>)root["message"]; ArrayList items = (ArrayList)msg["items"]; foreach (Dictionary<string, object> it in items) { string title = First(it, "title"); string venue = First(it, "container-title"); string url = GetString(it, "URL"); if (it.ContainsKey("link") && it["link"] is ArrayList links) { foreach (Dictionary<string, object> l in links) { string type = l.ContainsKey("content-type") ? Convert.ToString(l["content-type"]) : ""; string lurl = l.ContainsKey("URL") ? Convert.ToString(l["URL"]) : ""; if (!string.IsNullOrEmpty(lurl) && (type.Contains("pdf") || lurl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))) { url = lurl; break; } } } string authors = JoinAuthors(it); string year = GetYear(it); string doi = GetString(it, "DOI"); bool hasPdf = DetectPdf(it); string lang = GetString(it, "language"); t.Rows.Add(title, authors, venue, year, url, doi, hasPdf, lang, false); } return t; }

        // === TEMEL YARDIMCILAR ===
        private static DataTable CreateEmptyTable() { DataTable t = new DataTable(); t.Columns.Add("Title"); t.Columns.Add("Authors"); t.Columns.Add("Venue"); t.Columns.Add("Year"); t.Columns.Add("Url"); t.Columns.Add("Doi"); t.Columns.Add("HasPdf", typeof(bool)); t.Columns.Add("Language"); t.Columns.Add("InLibrary", typeof(bool)); return t; }
        private static bool DetectPdf(Dictionary<string, object> it) { try { if (it.ContainsKey("link") && it["link"] is ArrayList links) { foreach (Dictionary<string, object> l in links) { string type = l.ContainsKey("content-type") ? Convert.ToString(l["content-type"]) : ""; string lurl = l.ContainsKey("URL") ? Convert.ToString(l["URL"]) : ""; if (type.ToLowerInvariant().Contains("pdf") || (!string.IsNullOrEmpty(lurl) && lurl.ToLowerInvariant().EndsWith(".pdf"))) return true; } } if (it.ContainsKey("resource") && it["resource"] is Dictionary<string, object> res && res.ContainsKey("primary") && res["primary"] is Dictionary<string, object> prim) { string purl = prim.ContainsKey("URL") ? Convert.ToString(prim["URL"]) : ""; if (!string.IsNullOrEmpty(purl) && purl.ToLowerInvariant().EndsWith(".pdf")) return true; } } catch { } return false; }
        private static void EnsureColumns(DataTable t) { if (!t.Columns.Contains("InLibrary")) t.Columns.Add("InLibrary", typeof(bool)); if (!t.Columns.Contains("HasPdf")) t.Columns.Add("HasPdf", typeof(bool)); }
        private static string HttpGet(string url) { try { HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url); req.UserAgent = "Mozilla/5.0"; req.Timeout = 10000; using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) return sr.ReadToEnd(); } catch { return "{}"; } }
        private static string First(Dictionary<string, object> it, string key) { if (!it.ContainsKey(key) || it[key] == null) return ""; if (it[key] is ArrayList arr && arr.Count > 0) return Convert.ToString(arr[0]); return Convert.ToString(it[key]); }
        private static string GetString(Dictionary<string, object> it, string key) => it.ContainsKey(key) && it[key] != null ? Convert.ToString(it[key]) : "";
        private static string JoinAuthors(Dictionary<string, object> it) { if (!it.ContainsKey("author") || !(it["author"] is ArrayList)) return ""; var names = new List<string>(); foreach (Dictionary<string, object> a in (ArrayList)it["author"]) { string full = ((a.ContainsKey("given") ? a["given"] : "") + " " + (a.ContainsKey("family") ? a["family"] : "")).Trim(); if (full.Length > 1) names.Add(full); } return string.Join(", ", names); }
        private static string GetYear(Dictionary<string, object> it) { if (it.ContainsKey("published-print") && it["published-print"] is Dictionary<string, object> d && d.ContainsKey("date-parts")) { var parts = d["date-parts"] as ArrayList; if (parts != null && parts.Count > 0 && parts[0] is ArrayList y && y.Count > 0) return y[0].ToString(); } return ""; }
        private static int ParseInt(string s) => int.TryParse(s, out int v) ? v : 0;
        private static bool ContainsCI(string h, string n) => (h ?? "").IndexOf(n ?? "", StringComparison.OrdinalIgnoreCase) >= 0;
        private static string NormalizeDoi(string d) => (d ?? "").Replace("https://doi.org/", "").Replace("doi:", "").Trim();
        private bool IsInLibrary(string url) { if (string.IsNullOrEmpty(url)) return false; using (var c = new SqlConnection(Cs)) { c.Open(); var cmd = new SqlCommand("SELECT COUNT(*) FROM Library WHERE UserId=@u AND Url=@url", c); cmd.Parameters.AddWithValue("@u", CurrentUserId); cmd.Parameters.AddWithValue("@url", url); return (int)cmd.ExecuteScalar() > 0; } }
    }
}