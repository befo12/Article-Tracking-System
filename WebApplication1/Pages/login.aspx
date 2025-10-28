<%@ Page Title="Giriş" Language="C#"
    MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true"
    CodeBehind="Login.aspx.cs"
    Inherits="WebApplication1.Pages.LoginPage" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
  <div class="d-flex justify-content-center align-items-start" style="min-height:60vh;">
    <div class="card shadow-sm mt-5" style="width: 420px;">
      <div class="card-body">
        <h4 class="card-title mb-3">Giriş yap</h4>

        <asp:Label ID="lblMsg" runat="server" CssClass="text-danger d-block mb-2" />
        <asp:Label ID="lblDbg" runat="server" CssClass="text-muted small d-block mb-2" />

        <div class="mb-3">
          <label class="form-label">E-posta</label>
          <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" />
        </div>
        <div class="mb-3">
          <label class="form-label">Şifre</label>
          <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" />
        </div>

        <asp:Button ID="btnLogin" runat="server"
                    Text="Giriş"
                    CssClass="btn btn-primary w-100"
                    OnClick="btnLogin_Click" />

        <div class="d-flex justify-content-between mt-3 small">
          <span class="text-muted">Demo: <code>demo@site.local</code> / <code>123456</code></span>
          <a href="<%= ResolveUrl("~/Pages/Register.aspx") %>">Hesabın yok mu? Kayıt ol</a>
        </div>
      </div>
    </div>
  </div>
</asp:Content>
