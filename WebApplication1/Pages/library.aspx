<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true" CodeBehind="Library.aspx.cs"
    Inherits="WebApplication1.Pages.Library" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <h2 class="mb-3">Kütüphanem</h2>
  <asp:Label runat="server" ID="lblInfo" CssClass="mb-2 d-block text-muted" />

  <div class="row g-2 align-items-end mb-3">
    <div class="col-md-5">
      <label class="form-label">Anahtar kelimeler</label>
      <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control"
                   Placeholder="örn: slice 2mm NIST"></asp:TextBox>
    </div>

    <div class="col-md-2">
      <label class="form-label">Nerede ara?</label>
      <asp:DropDownList ID="ddlSearchIn" runat="server" CssClass="form-select">
        <asp:ListItem Text="Başlık" Value="title" />
        <asp:ListItem Text="Not" Value="note" />
        <asp:ListItem Text="Başlık + Not" Value="both" Selected="True" />
      </asp:DropDownList>
    </div>

    <div class="col-md-2">
      <label class="form-label">Eşleşme</label>
      <asp:DropDownList ID="ddlMatch" runat="server" CssClass="form-select">
        <asp:ListItem Text="Tüm kelimeler (AND)" Value="AND" Selected="True" />
        <asp:ListItem Text="Herhangi biri (OR)" Value="OR" />
      </asp:DropDownList>
    </div>

    <div class="col-md-2">
      <label class="form-label">Durum</label>
      <asp:DropDownList ID="ddlRead" runat="server" CssClass="form-select">
        <asp:ListItem Text="Hepsi" Value="" />
        <asp:ListItem Text="Sadece Okunmadı" Value="0" />
        <asp:ListItem Text="Sadece Okundu" Value="1" />
      </asp:DropDownList>
    </div>

    <div class="col-md-1">
      <asp:Button ID="btnSearch" runat="server" Text="Uygula"
                  CssClass="btn btn-outline-secondary w-100" OnClick="btnSearch_Click" />
    </div>
  </div>

  <asp:GridView runat="server" ID="gvLib" AutoGenerateColumns="False"
      CssClass="table table-striped table-sm"
      DataKeyNames="Id"
      OnRowCommand="gvLib_RowCommand"
      OnPageIndexChanging="gvLib_PageIndexChanging"
      AllowPaging="true" PageSize="10">

    <Columns>
  <%-- Başlık --%>
  <asp:BoundField DataField="Title" HeaderText="Başlık" />

  <%-- CrossRef Bağlantısı --%>
  <asp:HyperLinkField DataNavigateUrlFields="Url"
                      DataTextField="Url"
                      HeaderText="Bağlantı"
                      Target="_blank" />

  <asp:TemplateField HeaderText="Aç">
  <ItemTemplate>
    <!-- PDF varsa -->
    <asp:PlaceHolder ID="phPdf" runat="server"
      Visible='<%# !string.IsNullOrEmpty(Convert.ToString(Eval("PdfPath"))) %>'>
      <a class="btn btn-sm btn-outline-primary me-1" target="_blank"
         href='<%# "/SecureFile.ashx?id=" + Eval("Id") %>'>PDF Aç</a>
    </asp:PlaceHolder>

    <!-- HTML varsa -->
    <asp:PlaceHolder ID="phHtml" runat="server"
      Visible='<%# !string.IsNullOrEmpty(Convert.ToString(Eval("HtmlPath"))) %>'>
      <a class="btn btn-sm btn-outline-secondary me-1" target="_blank"
         href='<%# "/SecureFile.ashx?id=" + Eval("Id") %>'>HTML Aç</a>
    </asp:PlaceHolder>

    <!-- İkisi de yoksa -->
    <asp:PlaceHolder ID="phNone" runat="server"
      Visible='<%# string.IsNullOrEmpty(Convert.ToString(Eval("PdfPath"))) 
                && string.IsNullOrEmpty(Convert.ToString(Eval("HtmlPath"))) %>'>
      <span class="text-muted me-2">Yerel kopya yok</span>
      <a class="btn btn-sm btn-outline-dark" target="_blank"
         href='<%# Eval("Url") %>'>Orijinal</a>
    </asp:PlaceHolder>
  </ItemTemplate>
</asp:TemplateField>



  <%-- Not + Kaydet --%>
  <asp:TemplateField HeaderText="Not">
    <ItemTemplate>
      <div class="d-flex gap-2">
        <asp:TextBox ID="txtNote" runat="server" CssClass="form-control form-control-sm"
                     Text='<%# Bind("Note") %>' />
        <asp:LinkButton runat="server" CssClass="btn btn-sm btn-primary"
                        CommandName="saveNote"
                        CommandArgument='<%# Eval("Id") %>'>Kaydet</asp:LinkButton>
      </div>
    </ItemTemplate>
  </asp:TemplateField>

  <%-- Okundu --%>
  <asp:TemplateField HeaderText="Okundu">
    <ItemTemplate>
      <asp:CheckBox ID="chkRead" runat="server"
                    Checked='<%# Convert.ToBoolean(Eval("IsRead")) %>'
                    AutoPostBack="true"
                    OnCheckedChanged="ChkReadChanged" />
    </ItemTemplate>
  </asp:TemplateField>

  <%-- Sil --%>
  <asp:TemplateField HeaderText="İşlem">
    <ItemTemplate>
      <asp:LinkButton runat="server" CommandName="remove"
                      CommandArgument='<%# Eval("Id") %>'
                      CssClass="btn btn-sm btn-outline-danger">Sil</asp:LinkButton>
    </ItemTemplate>
  </asp:TemplateField>
</Columns>

  </asp:GridView>

</asp:Content>
