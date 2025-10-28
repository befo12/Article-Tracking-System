<%@ Page Title="Makale" Language="C#" MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true" CodeBehind="Article.aspx.cs" Inherits="WebApplication1.Pages.Article" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
  <div class="d-flex justify-content-between align-items-center mb-3">
    <h2 class="m-0"><asp:Label ID="lblTitle" runat="server" /></h2>
    <div class="btn-group">
      <a class="btn btn-outline-secondary" href="/Pages/Library.aspx">← Kütüphaneye dön</a>
      <asp:HyperLink ID="lnkDiscuss" runat="server" CssClass="btn btn-outline-primary">Tartış</asp:HyperLink>
    </div>
  </div>

  <div class="mb-2">
    <span class="badge bg-light text-dark border">Konu: <asp:Label ID="lblCat" runat="server" /></span>
  </div>

  <div class="card">
    <div class="card-body">
      <div class="form-check mb-3">
        <asp:CheckBox ID="chkRead" runat="server" CssClass="form-check-input" />
        <label class="form-check-label" for="chkRead">Okundu</label>
      </div>

      <div class="mb-3">
        <label class="form-label">Not</label>
        <asp:TextBox ID="txtNote" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" />
      </div>

      <div class="d-flex gap-2">
        <asp:Button ID="btnSave"   runat="server" Text="Kaydet" CssClass="btn btn-primary" OnClick="btnSave_Click" />
        <asp:Button ID="btnDelete" runat="server" Text="Kütüphaneden Kaldır" CssClass="btn btn-outline-danger"
                    OnClick="btnDelete_Click" OnClientClick="return confirm('Kaldırılsın mı?');" />
      </div>

      <asp:Label ID="lblInfo" runat="server" CssClass="text-success ms-2"></asp:Label>
    </div>
  </div>
</asp:Content>
