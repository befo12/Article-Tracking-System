<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master" AutoEventWireup="true" CodeBehind="Library.aspx.cs" Inherits="WebApplication1.Pages.Library" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:UpdatePanel ID="updLibrary" runat="server">
        <ContentTemplate>
            <style>
                .table td, .table th { vertical-align: middle !important; }
                .note-box { 
    width: 100%; 
    height: 38px; 
    font-size: 13px; 
    padding: 6px; 
    border: 1px solid #dee2e6; 
    border-radius: 6px; 
    background-color: #f8f9fa; 
    resize: none; 
    overflow: hidden; 
    text-overflow: ellipsis; 
    white-space: nowrap; 
    transition: all 0.2s ease; /* Daha yumuşak bir geçiş için */
}

.note-box:focus { 
    /* position: absolute;  <-- Soruna neden olan satırı sildik veya yorum satırı yaptık */
    min-height: 100px !important; /* Yüksekliği bulunduğu yerde artırır */
    background: white; 
    box-shadow: 0 4px 8px rgba(0,0,0,0.1); 
    border-color: #0d6efd; 
    outline: none; 
    white-space: normal; 
    overflow-y: auto; 
}
            </style>

            <h2 class="mb-3">Kütüphanem</h2>
            <asp:Label runat="server" ID="lblInfo" CssClass="mb-2 d-block text-muted" />

            <div class="row g-2 align-items-end mb-4 bg-light p-3 rounded">
                <div class="col-md-4">
                    <label class="form-label small fw-bold">Arama</label>
                    <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" Placeholder="Başlık veya not ara..."></asp:TextBox>
                </div>
                <div class="col-md-2">
                    <label class="form-label small">Nerede ara?</label>
                    <asp:DropDownList ID="ddlSearchIn" runat="server" CssClass="form-select">
                        <asp:ListItem Text="Başlık" Value="title" />
                        <asp:ListItem Text="Not" Value="note" />
                        <asp:ListItem Text="Her İkisi" Value="both" Selected="True" />
                    </asp:DropDownList>
                </div>
                <div class="col-md-2">
                    <label class="form-label small">Eşleşme</label>
                    <asp:DropDownList ID="ddlMatch" runat="server" CssClass="form-select">
                        <asp:ListItem Text="Hepsi (AND)" Value="AND" Selected="True" />
                        <asp:ListItem Text="Biri (OR)" Value="OR" />
                    </asp:DropDownList>
                </div>
                <div class="col-md-2">
                    <label class="form-label small">Durum</label>
                    <asp:DropDownList ID="ddlRead" runat="server" CssClass="form-select">
                        <asp:ListItem Text="Hepsi" Value="" />
                        <asp:ListItem Text="Okunmadı" Value="0" />
                        <asp:ListItem Text="Okundu" Value="1" />
                    </asp:DropDownList>
                </div>
                <div class="col-md-2">
                    <asp:Button ID="btnSearch" runat="server" Text="Filtrele" CssClass="btn btn-primary w-100" OnClick="btnSearch_Click" />
                </div>
            </div>

           <asp:GridView runat="server" ID="gvLib"
    AutoGenerateColumns="False"
    CssClass="table table-hover align-middle shadow-sm"
    DataKeyNames="Id"
    AllowPaging="true"
    PageSize="10"
    OnPageIndexChanging="gvLib_PageIndexChanging"
    OnRowCommand="gvLib_RowCommand"
    OnRowDataBound="gvLib_RowDataBound">

                <Columns>
                    <asp:BoundField DataField="Title" HeaderText="Başlık" ItemStyle-Width="30%" />
                    
                    <%-- 1. SÜTUN: OKUMA MODU (Read.aspx) --%>
<asp:TemplateField HeaderText="Okuma Modu">
    <ItemTemplate>
        <asp:HyperLink runat="server" CssClass="btn btn-sm btn-success" 
            NavigateUrl='<%# "~/Pages/Read.aspx?id=" + Eval("Id") %>'>
            <i class="fa fa-book-open"></i> Oku
        </asp:HyperLink>
    </ItemTemplate>
</asp:TemplateField>

<%-- 2. SÜTUN: DOSYA / KAYNAK LİNKLERİ --%>
<asp:TemplateField HeaderText="Kaynak Bağlantısı">
    <ItemTemplate>
        <div class="d-flex gap-1">
            <%-- PDF Varsa --%>
            <asp:PlaceHolder runat="server" 
                Visible='<%# Eval("PdfPath") != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(Eval("PdfPath"))) %>'>
                <a class="btn btn-sm btn-danger" target="_blank" href='<%# "/SecureFile.ashx?id=" + Eval("Id") %>'>PDF</a>
            </asp:PlaceHolder>

            <%-- HTML Varsa --%>
            <asp:PlaceHolder runat="server" 
                Visible='<%# Eval("HtmlPath") != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(Eval("HtmlPath"))) %>'>
                <a class="btn btn-sm btn-info text-white" target="_blank" href='<%# Eval("HtmlPath") %>'>HTML</a>
            </asp:PlaceHolder>

            <%-- Orijinal Link (Her zaman gösterilebilir veya diğerleri yoksa gösterilir) --%>
            <a class="btn btn-sm btn-outline-secondary" target="_blank" href='<%# Eval("Url") %>'>
                <%# Eval("Doi") != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(Eval("Doi"))) ? Eval("Doi") : "Link" %>
            </a>
        </div>
    </ItemTemplate>
</asp:TemplateField>
                    <asp:TemplateField HeaderText="Okundu">
    <ItemTemplate>
        <asp:CheckBox 
            ID="chkRead"
            runat="server"
            Checked='<%# Convert.ToBoolean(Eval("IsRead")) %>'
            AutoPostBack="true"
            OnCheckedChanged="ChkReadChanged" />
    </ItemTemplate>
</asp:TemplateField>

                    <asp:TemplateField HeaderText="Notlarım">
                        <ItemTemplate>
                            <div class="d-flex align-items-center gap-2">
                                <asp:TextBox ID="txtNote" runat="server" Text='<%# Bind("Note") %>' TextMode="MultiLine" CssClass="note-box" />
                                <asp:LinkButton runat="server" CommandName="saveNote" CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-sm btn-primary">💾</asp:LinkButton>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Puan">
                        <ItemTemplate>
                            <asp:DropDownList ID="ddlRating" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="RatingChanged" Width="75px">
                                <asp:ListItem Text="-" Value="0" />
                                <asp:ListItem Text="1" Value="1" />
                                <asp:ListItem Text="2" Value="2" />
                                <asp:ListItem Text="3" Value="3" />
                                <asp:ListItem Text="4" Value="4" />
                                <asp:ListItem Text="5" Value="5" />
                            </asp:DropDownList>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="İşlem">
                        <ItemTemplate>
                            <asp:LinkButton runat="server" CommandName="remove" CommandArgument='<%# Eval("Id") %>' 
                                CssClass="btn btn-sm btn-outline-danger" OnClientClick="return confirm('Kütüphaneden çıkartılsın mı?');">Kütüphaneden Çıkart</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>