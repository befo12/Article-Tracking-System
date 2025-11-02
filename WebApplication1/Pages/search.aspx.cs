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
    public partial class Search : Page
    {
        int CurrentUserId => Session["UserId"] == null ? 0 : (int)Session["UserId"];
        string Cs => ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        // Crossref’ten çekilen birleşik set
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

        // === ARA (ilk 50, cursor sakla) ===
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string q = (txtQuery.Text ?? "").Trim();
            if (q.Length == 0) return;

            var batch = FetchCrossrefBatch(q, pageSize: 50, cursor: "*", out string nextCursor);
            hfCursor.Value = nextCursor ?? "";

            EnsureColumns(batch);
            foreach (DataRow r in batch.Rows)
                r["InLibrary"] = IsInLibrary(Convert.ToString(r["Url"]));

            BaseTable = batch;
            gvResults.PageIndex = 0;
            ApplyFiltersAndBind();
            lblInfo.Text = $"{batch.Rows.Count} sonuç (ilk yükleme).";
        }

        // === DAHA FAZLA (cursor paging) ===
        protected void btnLoadMore_Click(object sender, EventArgs e)
        {
            if (BaseTable == null) return;
            string q = (txtQuery.Text ?? "").Trim();
            string cursor = hfCursor.Value;
            if (q.Length == 0 || string.IsNullOrEmpty(cursor)) return;

            var more = FetchCrossrefBatch(q, 50, cursor, out string nextCursor);
            hfCursor.Value = nextCursor ?? "";

            EnsureColumns(more);
            foreach (DataRow r in more.Rows)
                r["InLibrary"] = IsInLibrary(Convert.ToString(r["Url"]));

            // de-dup (DOI|URL)
            var all = BaseTable;
            var keys = new HashSet<string>(all.AsEnumerable()
                .Select(r => ((r["Doi"] + "|" + r["Url"]) ?? "").ToString().ToLowerInvariant()));

            foreach (DataRow add in more.Rows)
            {
                string k = ((add["Doi"] + "|" + add["Url"]) ?? "").ToString().ToLowerInvariant();
                if (!keys.Contains(k)) { all.ImportRow(add); keys.Add(k); }
            }

            BaseTable = all;
            ApplyFiltersAndBind();
            lblInfo.Text = $"Toplam {all.Rows.Count} sonuç (daha fazla eklendi).";
        }

        // === GRID ===
        protected void gvResults_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvResults.PageIndex = e.NewPageIndex;
            ApplyFiltersAndBind();
        }

        protected void gvResults_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            bool inLib = false;
            var key = gvResults.DataKeys[e.Row.RowIndex];
            if (key != null && key.Values["InLibrary"] != null)
                inLib = Convert.ToBoolean(key.Values["InLibrary"]);

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
                lblInfo.Text = "Kütüphaneden çıkarıldı: " + Server.HtmlEncode(title);
            }

            var t = BaseTable;
            var row = t?.AsEnumerable().FirstOrDefault(r => Convert.ToString(r["Url"]) == url);
            if (row != null) row["InLibrary"] = inLib;

            ApplyFiltersAndBind();
        }

        // === FİLTRELE & BAĞLA ===
        protected void FilterChanged(object sender, EventArgs e) => ApplyFiltersAndBind();

        private void ApplyFiltersAndBind()
        {
            var table = BaseTable;
            if (table == null)
            {
                gvResults.DataSource = null; gvResults.DataBind();
                lblInfo.Text = "Henüz sonuç yok.";
                return;
            }

            IEnumerable<DataRow> q = table.AsEnumerable();

            int yFrom = ParseInt(txtYearFrom.Text);
            int yTo = ParseInt(txtYearTo.Text);
            if (yFrom > 0) q = q.Where(r => ParseInt(r["Year"]?.ToString()) >= yFrom);
            if (yTo > 0) q = q.Where(r => ParseInt(r["Year"]?.ToString()) <= yTo);

            string author = (txtAuthor.Text ?? "").Trim();
            if (author.Length > 0) q = q.Where(r => ContainsCI(r["Authors"]?.ToString(), author));

            string venue = (txtVenue.Text ?? "").Trim();
            if (venue.Length > 0) q = q.Where(r => ContainsCI(r["Venue"]?.ToString(), venue));

            string exact = (txtExact.Text ?? "").Trim().Trim('"');
            if (chkExactPhrase.Checked && exact.Length > 0)
                q = q.Where(r => ContainsCI(r["Title"]?.ToString(), exact));

            if (chkHasPdf.Checked && table.Columns.Contains("HasPdf"))
                q = q.Where(r => r.Field<bool>("HasPdf") == true);

            if (chkInLibOnly.Checked)
                q = q.Where(r => r.Field<bool>("InLibrary") == true);

            switch (ddlSort.SelectedValue ?? "relevance")
            {
                case "date_desc": q = q.OrderByDescending(r => ParseInt(r["Year"]?.ToString())).ThenBy(r => r["Title"]); break;
                case "date_asc": q = q.OrderBy(r => ParseInt(r["Year"]?.ToString())).ThenBy(r => r["Title"]); break;
                case "title_asc": q = q.OrderBy(r => r["Title"]); break;
                default: break; // Crossref sırası ≈ alaka
            }

            var view = q.Any() ? q.CopyToDataTable() : table.Clone();
            gvResults.DataSource = view;
            gvResults.DataBind();

            lblInfo.Text = $"{view.Rows.Count} sonuç (listelenen), toplam {table.Rows.Count}.";
        }

        // === CROSSREF – tek batch (cursor) ===
        private DataTable FetchCrossrefBatch(string query, int pageSize, string cursor, out string nextCursor)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string url = "https://api.crossref.org/works" +
                         "?rows=" + pageSize +
                         "&query=" + HttpUtility.UrlEncode(query) +
                         "&cursor=" + HttpUtility.UrlEncode(cursor);

            string json = HttpGet(url);

            var ser = new JavaScriptSerializer();
            var root = ser.Deserialize<Dictionary<string, object>>(json);
            var msg = (Dictionary<string, object>)root["message"];
            nextCursor = msg.ContainsKey("next-cursor") ? Convert.ToString(msg["next-cursor"]) : null;

            return ParseCrossref(json);
        }

        // === Crossref JSON → DataTable (+ HasPdf) ===
        private static DataTable ParseCrossref(string json)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            Dictionary<string, object> root = ser.Deserialize<Dictionary<string, object>>(json);
            Dictionary<string, object> msg = (Dictionary<string, object>)root["message"];
            ArrayList items = (ArrayList)msg["items"];

            DataTable t = new DataTable();
            t.Columns.Add("Title");
            t.Columns.Add("Authors");
            t.Columns.Add("Venue");
            t.Columns.Add("Year");
            t.Columns.Add("Url");
            t.Columns.Add("Doi");
            t.Columns.Add("HasPdf", typeof(bool));

            foreach (Dictionary<string, object> it in items)
            {
                string title = First(it, "title");
                string venue = First(it, "container-title");
                string url = GetString(it, "URL");
                string authors = JoinAuthors(it);
                string year = GetYear(it);
                string doi = GetString(it, "DOI");
                bool hasPdf = DetectPdf(it);

                t.Rows.Add(title, authors, venue, year, url, doi, hasPdf);
            }
            return t;
        }

        private static bool DetectPdf(Dictionary<string, object> it)
        {
            try
            {
                if (it.ContainsKey("link") && it["link"] is ArrayList links)
                {
                    foreach (Dictionary<string, object> l in links)
                    {
                        string type = l.ContainsKey("content-type") ? Convert.ToString(l["content-type"]) : "";
                        string lurl = l.ContainsKey("URL") ? Convert.ToString(l["URL"]) : "";
                        if (type.ToLowerInvariant().Contains("pdf") ||
                            (!string.IsNullOrEmpty(lurl) && lurl.ToLowerInvariant().EndsWith(".pdf")))
                            return true;
                    }
                }
                if (it.ContainsKey("resource") && it["resource"] is Dictionary<string, object> res &&
                    res.ContainsKey("primary") && res["primary"] is Dictionary<string, object> prim)
                {
                    string purl = prim.ContainsKey("URL") ? Convert.ToString(prim["URL"]) : "";
                    if (!string.IsNullOrEmpty(purl) && purl.ToLowerInvariant().EndsWith(".pdf"))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static void EnsureColumns(DataTable t)
        {
            if (!t.Columns.Contains("InLibrary")) t.Columns.Add("InLibrary", typeof(bool));
            if (!t.Columns.Contains("HasPdf")) t.Columns.Add("HasPdf", typeof(bool));
        }

        // === DB işlemleri ===
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

                // PDF/HTML indir – Öncelik: DOI → HTML içi PDF → Crossref → en sonda HTML
                string err;
                string savedRel = TryFindAndDownloadBestCopy(doi, url, title, userId, out err);

                if (!string.IsNullOrEmpty(savedRel))
                {
                    string ext = Path.GetExtension(savedRel).ToLowerInvariant();
                    string sql = (ext == ".html" || ext == ".htm")
                        ? "UPDATE Library SET HtmlPath=@p WHERE Id=@id"
                        : "UPDATE Library SET PdfPath=@p WHERE Id=@id";

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

        // === PDF/HTML bulma sırası (DOI → HTML içi PDF → Crossref → en sonda HTML) ===
        private string TryFindAndDownloadBestCopy(string doi, string url, string title, int userId, out string error)
        {
            error = null;
            string nDoi = NormalizeDoi(doi);
            string firstHtmlRel = null; // PDF çıkmazsa en sonda döneceğiz

            bool TakeResult(string saved, string mime, bool isPdf, string e, out string ret)
            {
                ret = null;
                if (!string.IsNullOrEmpty(saved))
                {
                    if (isPdf) { ret = saved; return true; }          // PDF bulundu → hemen dön
                    if (firstHtmlRel == null) firstHtmlRel = saved;    // HTML'i kenara al, aramaya devam
                }
                if (!string.IsNullOrEmpty(e)) error = e;
                return false;
            }

            // 1) DOI (Accept: application/pdf) — PDF çıkarsa direkt indir, HTML ise yine indir (iç PDF aranır)
            {
                string s1, m1, e1; bool p1;
                if (TryDoiThenMaybeHtml_Inner(nDoi, title, userId, out s1, out m1, out p1, out e1))
                {
                    if (TakeResult(s1, m1, p1, e1, out string ret)) return ret;
                }
            }

            // 2) DOI/URL HTML ise — sayfanın içinden PDF yakala (href, data-*, window.open, meta refresh)
            {
                string s2, m2, e2; bool p2;
                string keyUrl = !string.IsNullOrWhiteSpace(nDoi) ? ("https://doi.org/" + nDoi) : url;
                if (!string.IsNullOrWhiteSpace(keyUrl))
                {
                    if (TryHtmlPageForEmbeddedPdf_Inner(keyUrl, title, userId, out s2, out m2, out p2, out e2))
                    {
                        if (TakeResult(s2, m2, p2, e2, out string ret)) return ret;
                    }
                }
            }

            // 3) Crossref kayıt linklerinden PDF
            {
                string s3, m3, e3; bool p3;
                string key = !string.IsNullOrWhiteSpace(nDoi) ? nDoi : url;
                if (TryCrossrefLinks_Inner(key, title, userId, out s3, out m3, out p3, out e3))
                {
                    if (TakeResult(s3, m3, p3, e3, out string ret)) return ret;
                }
            }

            // 4) En son çare: doğrudan orijinal URL — HTML'i kaydet (PDF bulunmasa da)
            {
                string s4, m4, e4; bool p4;
                if (!string.IsNullOrWhiteSpace(url) &&
                    DownloadToAppDataSmart(url, title, userId, keepHtmlIfNoPdf: true,
                                            out s4, out m4, out p4, out e4))
                {
                    if (p4) return s4; // PDF ise yine en iyi durum
                    if (firstHtmlRel == null) firstHtmlRel = s4;
                }
                if (!string.IsNullOrEmpty(e4)) error = e4;
            }

            // PDF yoksa eldeki en iyi HTML
            if (!string.IsNullOrEmpty(firstHtmlRel)) return firstHtmlRel;

            if (string.IsNullOrEmpty(error)) error = "no-source";
            return null;
        }

        // === DOI (Accept: application/pdf) + HTML’se de indir (iç PDF aranır) ===
        private bool TryDoiThenMaybeHtml_Inner(
            string normalizedDoi, string title, int userId,
            out string savedRel, out string mime, out bool isPdf, out string error)
        {
            savedRel = null; mime = null; isPdf = false; error = null;

            try
            {
                if (string.IsNullOrWhiteSpace(normalizedDoi)) { error = "doi:none"; return false; }
                string doiUrl = "https://doi.org/" + normalizedDoi;

                var req = (HttpWebRequest)WebRequest.Create(doiUrl);
                req.Method = "GET";
                req.AllowAutoRedirect = true;
                req.UserAgent = "Mozilla/5.0";
                req.Accept = "application/pdf"; // yayıncı PDF veriyorsa buradan gelir
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    string finalUrl = resp.ResponseUri?.ToString() ?? doiUrl;

                    // Her durumda indir; DownloadToAppDataSmart iç PDF’i kovalar.
                    return DownloadToAppDataSmart(finalUrl, title, userId,
                                                  keepHtmlIfNoPdf: true,
                                                  out savedRel, out mime, out isPdf, out error);
                }
            }
            catch (WebException wex) { error = "doi:webex:" + SafeWebEx(wex); return false; }
            catch (Exception ex) { error = "doi:ex:" + ex.Message; return false; }
        }

        // === HTML sayfasından gömülü PDF’i yakalama ===
        private bool TryHtmlPageForEmbeddedPdf_Inner(
            string pageUrl, string title, int userId,
            out string savedRel, out string mime, out bool isPdf, out string error)
        {
            return DownloadToAppDataSmart(pageUrl, title, userId,
                                          keepHtmlIfNoPdf: true,
                                          out savedRel, out mime, out isPdf, out error);
        }

        // === Crossref tekil kayıt linklerinden PDF ===
        private bool TryCrossrefLinks_Inner(
            string doiOrUrl, string title, int userId,
            out string savedRel, out string mime, out bool isPdf, out string error)
        {
            savedRel = null; mime = null; isPdf = false; error = null;

            try
            {
                if (string.IsNullOrEmpty(doiOrUrl)) { error = "crossref:no-key"; return false; }

                string apiUrl = "https://api.crossref.org/works/" + HttpUtility.UrlEncode(doiOrUrl);
                string json = HttpGet(apiUrl);

                var ser = new JavaScriptSerializer();
                var root = ser.Deserialize<Dictionary<string, object>>(json);
                if (root == null || !root.ContainsKey("message")) { error = "crossref:null-json"; return false; }

                var msg = root["message"] as Dictionary<string, object>;
                if (msg == null || !msg.ContainsKey("link")) { error = "crossref:no-link"; return false; }

                var links = msg["link"] as ArrayList;
                if (links == null || links.Count == 0) { error = "crossref:empty-link"; return false; }

                // Önce content-type=pdf olanı ara; yoksa .pdf ile biteni dene
                string candidate = null;
                foreach (Dictionary<string, object> l in links)
                {
                    string type = l.ContainsKey("content-type") ? Convert.ToString(l["content-type"]) : "";
                    string lurl = l.ContainsKey("URL") ? Convert.ToString(l["URL"]) : "";
                    if (!string.IsNullOrEmpty(type) && type.IndexOf("pdf", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        candidate = !string.IsNullOrEmpty(lurl) ? lurl : candidate;
                        break;
                    }
                }
                if (candidate == null)
                {
                    foreach (Dictionary<string, object> l in links)
                    {
                        string lurl = l.ContainsKey("URL") ? Convert.ToString(l["URL"]) : "";
                        if (!string.IsNullOrEmpty(lurl) && lurl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            candidate = lurl;
                            break;
                        }
                    }
                }
                if (candidate == null) { error = "crossref:no-pdf-link"; return false; }

                return DownloadToAppDataSmart(candidate, title, userId,
                                              keepHtmlIfNoPdf: true,
                                              out savedRel, out mime, out isPdf, out error);
            }
            catch (WebException wex) { error = "crossref:webex:" + SafeWebEx(wex); return false; }
            catch (Exception ex) { error = "crossref:ex:" + ex.Message; return false; }
        }

        // === içerik tipine göre kaydet + HTML→PDF fallback (yalnızca yeni imza) ===
        // keepHtmlIfNoPdf=true ise PDF bulunamazsa HTML'i de başarı sayıp döndürür (isPdf=false)
        private bool DownloadToAppDataSmart(
            string fileUrl, string title, int userId, bool keepHtmlIfNoPdf,
            out string savedRel, out string mime, out bool isPdf, out string error)
        {
            savedRel = null; mime = null; error = null; isPdf = false;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = false;

                string folderRel = "~/App_Data/Library/" + userId + "/";
                string folderAbs = Server.MapPath(folderRel);
                Directory.CreateDirectory(folderAbs);

                foreach (char c in Path.GetInvalidFileNameChars()) title = title.Replace(c, '_');
                if (title.Length > 100) title = title.Substring(0, 100);

                // URL’yi olası PDF’e çözümle (siteye özel kestirmeler dâhil)
                string resolvedUrl = ResolveToLikelyPdf(fileUrl, null, out _);

                var req = (HttpWebRequest)WebRequest.Create(resolvedUrl);
                req.Method = "GET";
                req.AllowAutoRedirect = true;
                req.Timeout = 25000;
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) WebFormsPDF/1.2";
                req.Accept = "application/pdf,text/html,application/xhtml+xml;q=0.9,*/*;q=0.8";
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var rs = resp.GetResponseStream())
                {
                    if (rs == null) { error = "null-stream"; return false; }

                    string ct = (resp.ContentType ?? "").ToLowerInvariant();

                    // 8 KB oku, %PDF imzası
                    byte[] head = new byte[8192];
                    int read = rs.Read(head, 0, head.Length);

                    bool looksPdf = false;
                    if (ct.Contains("pdf")) looksPdf = true;
                    else if (read >= 4 && head[0] == 0x25 && head[1] == 0x50 && head[2] == 0x44 && head[3] == 0x46) // %PDF
                        looksPdf = true;

                    // URL .pdf ile biterse, CT HTML gelse bile PDF say
                    if (!looksPdf && resp.ResponseUri != null &&
                        resp.ResponseUri.AbsoluteUri.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        looksPdf = true;

                    // dosya adı (Content-Disposition varsa kullan)
                    string suggestedName = title;
                    string disp = resp.Headers["Content-Disposition"] ?? "";
                    if (disp.IndexOf("filename=", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            string fn = disp.Split(new[] { "filename=" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim().Trim('"');
                            fn = Path.GetFileName(fn);
                            if (!string.IsNullOrWhiteSpace(fn))
                                suggestedName = Path.GetFileNameWithoutExtension(fn);
                        }
                        catch { }
                    }

                    if (looksPdf)
                    {
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
                        // HTML'i diske yaz (geçici/ya da fallback)
                        string absHtml = Path.Combine(folderAbs, suggestedName + ".html");
                        using (FileStream fs = File.Create(absHtml))
                        {
                            if (read > 0) fs.Write(head, 0, read);
                            rs.CopyTo(fs);
                        }

                        try
                        {
                            string html = File.ReadAllText(absHtml, Encoding.UTF8);
                            string baseUrl = resp.ResponseUri?.ToString() ?? fileUrl;

                            // a) href="...pdf"
                            var mHref = System.Text.RegularExpressions.Regex.Match(
                                html, @"href\s*=\s*[""'](?<u>[^""']+\.pdf(\?[^""']*)?)[""']",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (mHref.Success)
                            {
                                string rel = mHref.Groups["u"].Value;
                                string abs2 = new Uri(new Uri(baseUrl), rel).ToString();
                                string s2, m2, e2; bool p2;
                                if (DownloadToAppDataSmart(abs2, title, userId, keepHtmlIfNoPdf, out s2, out m2, out p2, out e2))
                                {
                                    if (p2) { try { File.Delete(absHtml); } catch { } }
                                    savedRel = s2; mime = m2; isPdf = p2; error = null;
                                    return true;
                                }
                            }

                            // b) data-href / data-url
                            var mData = System.Text.RegularExpressions.Regex.Match(
                                html, @"data-(href|url)\s*=\s*[""'](?<u>[^""']+\.pdf(\?[^""']*)?)[""']",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (mData.Success)
                            {
                                string rel = mData.Groups["u"].Value;
                                string abs2 = new Uri(new Uri(baseUrl), rel).ToString();
                                string s2, m2, e2; bool p2;
                                if (DownloadToAppDataSmart(abs2, title, userId, keepHtmlIfNoPdf, out s2, out m2, out p2, out e2))
                                {
                                    if (p2) { try { File.Delete(absHtml); } catch { } }
                                    savedRel = s2; mime = m2; isPdf = p2; error = null;
                                    return true;
                                }
                            }

                            // c) window.open('...pdf') / location.href='...pdf'
                            var mJs = System.Text.RegularExpressions.Regex.Match(
                                html,
                                @"window\.open\s*\(\s*['""](?<u>[^'""]+\.pdf(\?[^'""]*)?)['""]\s*\)|location\.href\s*=\s*['""](?<u2>[^'""]+\.pdf(\?[^'""]*)?)['""]",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (mJs.Success)
                            {
                                string rel = mJs.Groups["u"].Success ? mJs.Groups["u"].Value : mJs.Groups["u2"].Value;
                                string abs2 = new Uri(new Uri(baseUrl), rel).ToString();
                                string s2, m2, e2; bool p2;
                                if (DownloadToAppDataSmart(abs2, title, userId, keepHtmlIfNoPdf, out s2, out m2, out p2, out e2))
                                {
                                    if (p2) { try { File.Delete(absHtml); } catch { } }
                                    savedRel = s2; mime = m2; isPdf = p2; error = null;
                                    return true;
                                }
                            }

                            // d) meta refresh ile PDF
                            var mMeta = System.Text.RegularExpressions.Regex.Match(
                                html, @"http-equiv\s*=\s*[""']refresh[""'][^>]*url=(?<u>[^'""\s>]+)",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (mMeta.Success)
                            {
                                string rel = mMeta.Groups["u"].Value;
                                string abs2 = new Uri(new Uri(baseUrl), rel).ToString();
                                if (abs2.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                                {
                                    string s2, m2, e2; bool p2;
                                    if (DownloadToAppDataSmart(abs2, title, userId, keepHtmlIfNoPdf, out s2, out m2, out p2, out e2))
                                    {
                                        if (p2) { try { File.Delete(absHtml); } catch { } }
                                        savedRel = s2; mime = m2; isPdf = p2; error = null;
                                        return true;
                                    }
                                }
                            }
                        }
                        catch { /* sessiz */ }

                        // HTML bulundu; PDF yok. İsteme bağlı olarak HTML'i başarı say.
                        if (keepHtmlIfNoPdf)
                        {
                            savedRel = folderRel + suggestedName + ".html";
                            mime = "text/html";
                            isPdf = false;
                            return true;
                        }
                        else
                        {
                            try { File.Delete(Path.Combine(folderAbs, suggestedName + ".html")); } catch { }
                            error = "html-only";
                            return false;
                        }
                    }
                }
            }
            catch (WebException wex) { error = "webex:" + SafeWebEx(wex); return false; }
            catch (Exception ex) { error = "ex:" + ex.Message; return false; }
        }

        // Landing URL'yi doğrudan PDF'e çevirmeyi dener (arXiv / DergiPark / OSTI gibi)
        private static string MaybeDirectPdfUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                var u = new Uri(url);
                string host = u.Host.ToLowerInvariant();

                // arXiv: /abs/XXXX -> /pdf/XXXX.pdf
                if (host.Contains("arxiv.org"))
                {
                    if (u.AbsolutePath.StartsWith("/abs/", StringComparison.OrdinalIgnoreCase))
                    {
                        string id = u.AbsolutePath.Substring("/abs/".Length);
                        return "https://arxiv.org/pdf/" + id + ".pdf";
                    }
                }

                // DergiPark: çoğu sayfada /download/article-file/{id}
                if (host.Contains("dergipark.org.tr"))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(url, @"article-file/(\d+)",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success)
                        return "https://dergipark.org.tr/tr/download/article-file/" + m.Groups[1].Value;

                    if (!u.AbsolutePath.EndsWith("/download", StringComparison.OrdinalIgnoreCase))
                        return url.TrimEnd('/') + "/download";
                }

                // OSTI: çoğu zaman zaten .pdf
                if (host.Contains("osti.gov") && u.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return url;

                // URL zaten .pdf ise
                if (url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return url;

                return null;
            }
            catch { return null; }
        }

        // URL gerçekten PDF mi? (tek tanım) — önce siteye özel kestirme, sonra hafif GET
        private string ResolveToLikelyPdf(string originalUrl, string normalizedDoi, out string debug)
        {
            debug = null;
            try
            {
                if (string.IsNullOrWhiteSpace(originalUrl)) return originalUrl;

                // 0) Siteye özel kestirmeler
                string direct = MaybeDirectPdfUrl(originalUrl);
                if (!string.IsNullOrEmpty(direct)) return direct;

                // 1) Zaten .pdf ile bitiyorsa
                if (originalUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return originalUrl;

                // 2) Küçük GET – yönlendirme sonu & content-type kontrol
                var req = (HttpWebRequest)WebRequest.Create(originalUrl);
                req.Method = "GET";
                req.AllowAutoRedirect = true;
                req.UserAgent = "Mozilla/5.0";
                req.Accept = "application/pdf,text/html,application/xhtml+xml;q=0.9,*/*;q=0.8";
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                req.Timeout = 12000;

                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    string final = resp.ResponseUri?.ToString() ?? originalUrl;
                    string ct = (resp.ContentType ?? "").ToLowerInvariant();
                    if (ct.Contains("pdf") || final.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        return final;
                }
            }
            catch (Exception ex) { debug = "resolve-ex:" + ex.Message; }
            return originalUrl;
        }

        // Library’de var mı? (URL bazlı)
        private bool IsInLibrary(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            using (SqlConnection con = new SqlConnection(Cs))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT 1 FROM Library WHERE UserId=@uid AND Url=@u", con))
            {
                cmd.Parameters.AddWithValue("@uid", CurrentUserId);
                cmd.Parameters.AddWithValue("@u", (object)url ?? DBNull.Value);
                con.Open();
                object v = cmd.ExecuteScalar();
                return v != null;
            }
        }

        // === yardımcılar ===
        private static string SafeWebEx(WebException wex)
        {
            try { var resp = (HttpWebResponse)wex.Response; if (resp != null) return ((int)resp.StatusCode) + " " + resp.StatusDescription; }
            catch { }
            return wex.Message;
        }

        private static string HttpGet(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "WebFormsDemo/1.0";
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static string First(Dictionary<string, object> it, string key)
        {
            if (!it.ContainsKey(key) || it[key] == null) return "";
            ArrayList arr = it[key] as ArrayList;
            if (arr != null && arr.Count > 0) return Convert.ToString(arr[0]);
            return Convert.ToString(it[key]);
        }
        private static string GetString(Dictionary<string, object> it, string key)
            => it.ContainsKey(key) && it[key] != null ? Convert.ToString(it[key]) : "";

        private static string JoinAuthors(Dictionary<string, object> it)
        {
            if (!it.ContainsKey("author") || it["author"] == null) return "";
            ArrayList arr = it["author"] as ArrayList; if (arr == null) return "";
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
            string[] keys = new[] { "published-print", "published-online", "issued" };
            foreach (string key in keys)
            {
                if (!it.ContainsKey(key) || it[key] == null) continue;
                Dictionary<string, object> pub = it[key] as Dictionary<string, object>;
                if (pub == null || !pub.ContainsKey("date-parts")) continue;
                ArrayList outer = pub["date-parts"] as ArrayList; if (outer == null || outer.Count == 0) continue;
                ArrayList inner = outer[0] as ArrayList; if (inner == null || inner.Count == 0) continue;
                return Convert.ToString(inner[0]);
            }
            return "";
        }

        private static int ParseInt(string s) => int.TryParse((s ?? "").Trim(), out int v) ? v : 0;

        private static bool ContainsCI(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(needle)) return true;
            if (string.IsNullOrEmpty(haystack)) return false;
            return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeDoi(string doi)
        {
            if (string.IsNullOrWhiteSpace(doi)) return "";
            doi = doi.Trim();
            if (doi.StartsWith("http://doi.org/", StringComparison.OrdinalIgnoreCase))
                doi = doi.Substring("http://doi.org/".Length);
            else if (doi.StartsWith("https://doi.org/", StringComparison.OrdinalIgnoreCase))
                doi = doi.Substring("https://doi.org/".Length);
            else if (doi.StartsWith("doi:", StringComparison.OrdinalIgnoreCase))
                doi = doi.Substring("doi:".Length);
            return doi.Trim();
        }
    }
}
