<%@ Page Title="Profil" Language="C#"
    MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true"
    CodeBehind="profile.aspx.cs"
    Inherits="WebApplication1.Pages.ProfilePage" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
  <h2>Profil</h2>
  <p><b>Ad:</b> <asp:Literal ID="litName" runat="server" /> &nbsp; | &nbsp;
     <b>E-posta:</b> <asp:Literal ID="litEmail" runat="server" /></p>

  <h4>Anahtar Kelimeler</h4>
  <div class="input-group mb-3" style="max-width:520px">
    <asp:TextBox ID="txtKeyword" runat="server" CssClass="form-control" placeholder="ör. AI, physics" />
    <asp:Button ID="btnAddKeyword" runat="server" CssClass="btn btn-primary" Text="Ekle" OnClick="btnAddKeyword_Click" />
  </div>

  <asp:GridView ID="gvKeywords" runat="server" AutoGenerateColumns="false"
      CssClass="table table-striped" DataKeyNames="Id"
      OnRowCommand="gvKeywords_RowCommand">
    <Columns>
      <asp:BoundField DataField="Keyword" HeaderText="Anahtar Kelime" />
      <asp:TemplateField>
        <ItemTemplate>
          <asp:LinkButton ID="btnDel" runat="server" Text="Sil"
                          CommandName="Del" CommandArgument='<%# Container.DataItemIndex %>' />
        </ItemTemplate>
      </asp:TemplateField>
    </Columns>
  </asp:GridView>

  <asp:Label ID="lblInfo" runat="server" CssClass="text-muted"></asp:Label>
</asp:Content>
