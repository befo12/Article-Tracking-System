<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true" CodeBehind="Home.aspx.cs"
    Inherits="WebApplication1.Pages.Home" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
  <h2 class="mb-1">Hoş geldin, <%: Name %>!</h2>
  <div class="text-muted mb-3">E-posta: <%: Email %></div>

  <div class="card mb-4">
    <div class="card-body">
      <h5 class="card-title">Hızlı Ekle</h5>
      <div class="row g-2 align-items-center">
        <div class="col-md-5">
          <asp:TextBox runat="server" ID="txtQuickTitle" CssClass="form-control" placeholder="Başlık" />
        </div>
        <div class="col-md-5">
          <asp:TextBox runat="server" ID="txtQuickUrl" CssClass="form-control" placeholder="https://…" />
        </div>
        <div class="col-md-2 text-end">
          <asp:Button runat="server" ID="btnQuickAdd" CssClass="btn btn-primary w-100"
                      Text="Kütüphaneye Ekle" OnClick="btnQuickAdd_Click" />
        </div>
      </div>
      <div class="mt-2">
        <asp:TextBox runat="server" ID="txtQuickNote" CssClass="form-control" TextMode="MultiLine"
                     placeholder="Not (isteğe bağlı)" Rows="2" />
      </div>
      <small class="text-muted">İpucu: Başlık/URL’yi yapıştır.</small>
      <div><asp:Label runat="server" ID="lblQuickMsg" /></div>
    </div>
  </div>

  <h4 class="mb-2">Haftalık Öneriler</h4>
  <asp:Repeater runat="server" ID="rptWeekly">
    <ItemTemplate>
      <div class="mb-2">
        <a href='<%# Eval("Url") %>' target="_blank" class="link-primary fw-semibold"><%# Eval("Title") %></a>
        <div class="text-muted"><%# Eval("Meta") %></div>
      </div>
    </ItemTemplate>
  </asp:Repeater>

  <h4 class="mt-4 mb-2">Son etkinlikler</h4>
  <asp:Repeater runat="server" ID="rptFeed">
    <ItemTemplate>
      <div class="card mb-2">
        <div class="card-body">
          <div class="fw-semibold"><%# Eval("Title") %></div>
          <div class="text-muted small"><%# String.Format("{0:dd.MM.yyyy HH:mm}", Eval("When")) %> — <%# Eval("User") %></div>
          <div><%# Eval("Text") %></div>
        </div>
      </div>
    </ItemTemplate>
  </asp:Repeater>
</asp:Content>
