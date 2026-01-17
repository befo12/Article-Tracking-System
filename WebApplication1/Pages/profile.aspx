<%@ Page Title="Profil" Language="C#"
    MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true"
    CodeBehind="profile.aspx.cs"
    Inherits="WebApplication1.Pages.ProfilePage"
    Async="true" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h2 class="mb-4">Profil Paneli</h2>

    <div class="row mb-4">
        <div class="col-md-4">
            <div class="card bg-primary text-white shadow-sm">
                <div class="card-body text-center">
                    <h6>Favori Makaleler</h6>
                    <h2 class="mb-0"><asp:Literal ID="litFavCount" runat="server">0</asp:Literal></h2>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card bg-success text-white shadow-sm">
                <div class="card-body text-center">
                    <h6>Toplam Arama</h6>
                    <h2 class="mb-0"><asp:Literal ID="litSearchCount" runat="server">0</asp:Literal></h2>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card bg-info text-white shadow-sm">
                <div class="card-body text-center">
                    <h6>Puanlamalarım</h6>
                    <h2 class="mb-0"><asp:Literal ID="litRateCount" runat="server">0</asp:Literal></h2>
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm mb-4">
        <div class="card-body">
            <p class="mb-0"><b>Ad:</b> <asp:Literal ID="litName" runat="server" /> &nbsp; | &nbsp;
               <b>E-posta:</b> <asp:Literal ID="litEmail" runat="server" /></p>
        </div>
    </div>

    <div class="card shadow-sm">
        <div class="card-header bg-dark text-white">
            <h5 class="mb-0">İlgi Alanlarım (Anahtar Kelimeler)</h5>
        </div>
        <div class="card-body">
            <%-- Kelime Ekleme Formu --%>
            <div class="input-group mb-3" style="max-width:500px">
                <asp:TextBox ID="txtKeyword" runat="server" CssClass="form-control" 
                    placeholder="Örn: AI, Quantum Physics" />
                <asp:Button ID="btnAddKeyword" runat="server" Text="Ekle" 
                    CssClass="btn btn-primary" OnClick="btnAddKeyword_Click" />
            </div>
            
            <%-- Kelime Listesi (Repeater ile) --%>
            <div class="d-flex flex-wrap gap-2">
                <asp:Repeater ID="rptKeywords" runat="server" OnItemCommand="rptKeywords_ItemCommand">
                    <ItemTemplate>
                        <span class="badge bg-secondary p-2 d-flex align-items-center">
                            <%# Eval("Keyword") %>
                            <asp:LinkButton runat="server" 
                                CommandName="Delete" 
                                CommandArgument='<%# Eval("Id") %>'
                                CssClass="btn-close btn-close-white ms-2" 
                                style="font-size:10px"
                                OnClientClick="return confirm('Bu kelimeyi silmek istediğinize emin misiniz?');" />
                        </span>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>