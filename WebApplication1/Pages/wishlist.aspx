<%@ Page Language="C#" %><%@ Import Namespace="System" %><%@ Import Namespace="System.Data" %>
<!DOCTYPE html><html lang="tr"><head runat="server">
<meta charset="utf-8"/><title>Okumak İstediklerim</title>
<meta name="viewport" content="width=device-width, initial-scale=1"/>
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet"/></head>
<body class="container py-4">
<script runat="server">
  DataTable T(){ return (DataTable)(Session["articles"] ?? new DataTable()); }
  protected void Page_Load(object s, EventArgs e){
    // işlemler
    if (Request.HttpMethod=="POST"){
      var t=T(); int id=int.Parse(Request.Form["id"]);
      foreach(DataRow r in t.Rows){
        if((int)r["Id"]==id){
          if(Request.Form["action"]=="unsave") r["Saved"]=false;
          if(Request.Form["action"]=="markread"){ r["Read"]=true; r["LastReadAt"]=DateTime.UtcNow; }
          break;
        }
      }
      Session["articles"]=t; Response.Redirect("wishlist.aspx"); return;
    }
  }
</script>

<h3 class="mb-3">Okumak İstediklerim</h3>
<% var data = T(); foreach (DataRow r in data.Select("Saved = true")) { %>
  <div class="card p-3 mb-2">
    <div class="fw-semibold"><%= r["Title"] %></div>
    <div class="small text-secondary"><%= r["Summary"] %></div>
    <div class="mt-2 d-flex gap-2">
      <a class="btn btn-sm btn-outline-primary" target="_blank" href="<%= r["Url"] %>">Aç</a>
      <form method="post" class="d-inline">
        <input type="hidden" name="id" value="<%= r["Id"] %>" />
        <input type="hidden" name="action" value="markread" />
        <button class="btn btn-sm btn-success">Okundu say</button>
      </form>
      <form method="post" class="d-inline">
        <input type="hidden" name="id" value="<%= r["Id"] %>" />
        <input type="hidden" name="action" value="unsave" />
        <button class="btn btn-sm btn-outline-danger">Listeden çıkar</button>
      </form>
    </div>
  </div>
<% } %>

<a class="btn btn-link mt-3" href="hub.aspx">← Merkez</a>
</body></html>
