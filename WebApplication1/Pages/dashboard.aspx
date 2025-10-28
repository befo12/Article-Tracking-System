<%@ Page Title="Panel" Language="C#"
    MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true"
    CodeBehind="Dashboard.aspx.cs"
    Inherits="WebApplication1.Pages.Dashboard" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
  <h3 class="mb-3">Öneriler</h3>

  <asp:Label ID="lblInfo" runat="server" CssClass="text-muted d-block mb-3" />

  <!-- Dış grup (etiket/kategori) için repeater -->
  <asp:Repeater ID="repGroups" runat="server">
    <ItemTemplate>
      <div class="card mb-3">
        <div class="card-header fw-semibold"><%# Eval("Tag") %></div>
        <div class="card-body p-0">
          <!-- İçteki öğeler -->
          <asp:Repeater ID="repItems" runat="server" OnItemCommand="RepItems_ItemCommand">
            <HeaderTemplate>
              <ul class="list-group list-group-flush">
            </HeaderTemplate>
            <ItemTemplate>
              <li class="list-group-item d-flex justify-content-between align-items-start">
                <div class="me-3">
                  <div class="fw-semibold"><%# Eval("Title") %></div>
                  <div class="small text-muted"><%# Eval("Summary") %></div>
                  <a class="small" href="<%# Eval("Url") %>" target="_blank" rel="noopener">Bağlantı</a>
                </div>
                <asp:LinkButton runat="server"
                                CommandName="add"
                                CommandArgument='<%# Eval("Title") + "||" + Eval("Url") %>'
                                CssClass="btn btn-sm btn-outline-primary">Kütüphaneye ekle</asp:LinkButton>
              </li>
            </ItemTemplate>
            <FooterTemplate>
              </ul>
            </FooterTemplate>
          </asp:Repeater>
        </div>
      </div>
    </ItemTemplate>
  </asp:Repeater>
</asp:Content>
