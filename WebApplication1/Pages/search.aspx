<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master" AutoEventWireup="true"
    CodeBehind="search.aspx.cs" Inherits="WebApplication1.Pages.Search" %>

<asp:Content ID="title" ContentPlaceHolderID="TitleContent" runat="server">Makale Ara</asp:Content>

<asp:Content ID="main" ContentPlaceHolderID="MainContent" runat="server">
  <div class="mb-3">
    <div class="input-group">
      <asp:TextBox runat="server" ID="txtQuery" CssClass="form-control" placeholder="Makale ara…" />
      <asp:Button runat="server" ID="btnSearch" CssClass="btn btn-primary" Text="Ara" OnClick="btnSearch_Click" />
    </div>
    <small class="text-muted">Üst barda aradığında da bu sayfaya düşer.</small>
  </div>

  <asp:Label runat="server" ID="lblInfo" CssClass="mb-2 d-block" />

  <asp:GridView runat="server" ID="gvResults"
      CssClass="table table-sm table-striped"
      AutoGenerateColumns="False"
      DataKeyNames="Title,Url,InLibrary"
      OnRowCommand="gvResults_RowCommand"
      OnRowDataBound="gvResults_RowDataBound">
    <Columns>
      <asp:BoundField DataField="Title" HeaderText="Başlık" />
      <asp:BoundField DataField="Authors" HeaderText="Yazarlar" />
      <asp:BoundField DataField="Venue" HeaderText="Dergi/Konferans" />
      <asp:BoundField DataField="Year" HeaderText="Yıl" />
      <asp:HyperLinkField DataNavigateUrlFields="Url" DataTextField="Url" HeaderText="Bağlantı" />
      <asp:TemplateField HeaderText="İşlem">
        <ItemTemplate>
          <asp:LinkButton runat="server" ID="btnAdd"
              CommandName="AddToLib"
              CommandArgument="<%# Container.DataItemIndex %>"
              CssClass="btn btn-sm btn-outline-primary me-2"
              Text="Kütüphaneme Ekle" />
          <asp:LinkButton runat="server" ID="btnRemove"
              CommandName="RemoveFromLib"
              CommandArgument="<%# Container.DataItemIndex %>"
              CssClass="btn btn-sm btn-outline-danger"
              Text="Kütüphaneden Çıkar" />
        </ItemTemplate>
      </asp:TemplateField>
    </Columns>
  </asp:GridView>
</asp:Content>
