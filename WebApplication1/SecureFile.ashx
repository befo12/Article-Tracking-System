<%@ WebHandler Language="C#" Class="SecureFile" %>
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.SessionState;

public class SecureFile : IHttpHandler, IRequiresSessionState
{
    private string Cs
    {
        get { return ConfigurationManager.ConnectionStrings["Db"].ConnectionString; }
    }

    public void ProcessRequest(HttpContext ctx)
    {
        // 1) login kontrol
        int uid = (ctx.Session["UserId"] == null) ? 0 : (int)ctx.Session["UserId"];
        if (uid == 0) { Forbidden(ctx, "not-auth"); return; }

        // 2) id
        int id;
        string sid = ctx.Request["id"];
        if (!int.TryParse(sid, out id) || id <= 0) { NotFound(ctx, "bad-id"); return; }

        // 3) DB'den yollar
        string title = "paper";
        string pdfRel = null;
        string htmlRel = null;
        string url = null;

        using (var con = new SqlConnection(Cs))
        using (var cmd = new SqlCommand(
            "SELECT Title, PdfPath, HtmlPath, Url FROM Library WHERE Id=@id AND UserId=@uid", con))
        {
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@uid", uid);
            con.Open();
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    title  = Convert.ToString(r["Title"] ?? "paper");
                    pdfRel = Convert.ToString(r["PdfPath"] ?? "");
                    htmlRel= Convert.ToString(r["HtmlPath"] ?? "");
                    url    = Convert.ToString(r["Url"] ?? "");
                }
                else { NotFound(ctx, "no-row"); return; }
            }
        }

        // 4) Öncelik: PDF → HTML → URL
        if (!string.IsNullOrEmpty(pdfRel))
        {
            string absPdf = ToAbs(ctx, pdfRel);
            if (File.Exists(absPdf)) { StreamFile(ctx, absPdf, title, "application/pdf", ".pdf"); return; }
        }

        if (!string.IsNullOrEmpty(htmlRel))
        {
            string absHtml = ToAbs(ctx, htmlRel);
            if (File.Exists(absHtml)) { StreamFile(ctx, absHtml, title, "text/html; charset=utf-8", ".html"); return; }
        }

        if (!string.IsNullOrEmpty(url))
        {
            ctx.Response.Clear();
            ctx.Response.Redirect(url, true);
            return;
        }

        NotFound(ctx, "no-source");
    }

    public bool IsReusable { get { return false; } }

    // ===== yardımcılar (C# 5 uyumlu) =====
    private static string ToAbs(HttpContext ctx, string rel)
    {
        if (string.IsNullOrEmpty(rel)) return null;
        if (rel.StartsWith("~")) return ctx.Server.MapPath(rel);
        return Path.Combine(ctx.Server.MapPath("~"), rel.TrimStart('/', '\\'));
    }

    private static void StreamFile(HttpContext ctx, string abs, string title, string contentType, string defExt)
    {
        string safe = MakeSafe(title);
        string ext = Path.GetExtension(abs);
        if (string.IsNullOrEmpty(ext)) ext = defExt;

        ctx.Response.Clear();
        ctx.Response.ContentType = contentType;
        ctx.Response.AddHeader("Content-Disposition", "inline; filename=\"" + safe + ext + "\"");
        ctx.Response.AddHeader("Content-Length", new FileInfo(abs).Length.ToString());
        ctx.Response.TransmitFile(abs);
        ctx.Response.End();
    }

    private static void NotFound(HttpContext c, string why)
    {
        c.Response.StatusCode = 404;
        c.Response.ContentType = "text/plain; charset=utf-8";
        c.Response.Write("404: " + why);
        c.Response.End();
    }

    private static void Forbidden(HttpContext c, string why)
    {
        c.Response.StatusCode = 403;
        c.Response.ContentType = "text/plain; charset=utf-8";
        c.Response.Write("403: " + why);
        c.Response.End();
    }

    private static string MakeSafe(string s)
    {
        if (string.IsNullOrEmpty(s)) return "file";
        foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
        if (s.Length > 80) s = s.Substring(0, 80);
        return s;
    }
}
