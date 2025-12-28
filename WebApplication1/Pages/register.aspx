<%@ Page  Async="true"  Title="Kayıt Ol" Language="C#"
    MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true"
    CodeBehind="Register.aspx.cs"
    Inherits="WebApplication1.Pages.RegisterPage" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
  <div class="d-flex justify-content-center align-items-start" style="min-height:60vh;">
    <div class="card shadow-sm mt-5" style="width: 460px;">
      <div class="card-body">
        <h4 class="card-title mb-3">Kayıt ol</h4>

        <asp:Label ID="lblMsg" runat="server" CssClass="d-block mb-2"></asp:Label>

        <div class="mb-3">
          <label class="form-label">İsim (isteğe bağlı)</label>
          <asp:TextBox ID="txtName" runat="server" CssClass="form-control" />
        </div>
        <div class="mb-3">
          <label class="form-label">E-posta</label>
          <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" />
        </div>
        <div class="mb-3">
          <label class="form-label">Şifre</label>
          <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" />
        </div>

        <asp:Button ID="btnRegister" runat="server" Text="Kayıt Ol"
                    CssClass="btn btn-success w-100" OnClick="btnRegister_Click" />

        <div class="mt-3 small">
          Zaten hesabın var mı? <a href="~/Pages/Login.aspx">Giriş yap</a>
        </div>
      </div>
    </div>
  </div>
</asp:Content>
