<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="WebApplication1.Pages.Dashboard" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <style>
        .topic-section { margin-bottom: 2rem; }
        .topic-header { 
            background: #f8f9fa; border-left: 5px solid #0d6efd; 
            padding: 10px 15px; margin-bottom: 1rem; border-radius: 0 5px 5px 0;
            font-size: 1.2rem; font-weight: bold; color: #333;
        }
        .dash-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
            gap: 1rem;
        }
        .dash-card {
            background: #fff; border: 1px solid #eee; border-radius: 8px;
            padding: 1rem; display: flex; flex-direction: column;
            transition: 0.2s; box-shadow: 0 2px 5px rgba(0,0,0,0.05);
        }
        .dash-card:hover { transform: translateY(-3px); border-color: #0d6efd; }
        .dash-title { font-weight: 600; color: #2c3e50; text-decoration: none; margin-bottom: 0.5rem; display: block; }
        .dash-meta { font-size: 0.85rem; color: #777; margin-bottom: 1rem; flex-grow: 1; }
        .dash-footer { margin-top: auto; text-align: right; border-top: 1px solid #f0f0f0; padding-top: 10px; }
    </style>

    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="fw-bold m-0">Önerilen Makaleler</h2>
        <span class="badge bg-info text-dark"><asp:Literal ID="lblInfo" runat="server" /></span>
    </div>

    <asp:UpdatePanel runat="server">
        <ContentTemplate>
            
            <%-- DIŞ DÖNGÜ: KONULAR --%>
            <asp:Repeater ID="repGroups" runat="server" OnItemDataBound="repGroups_ItemDataBound">
                <ItemTemplate>
                    <div class="topic-section">
                        <div class="topic-header"><%# Eval("Tag") %></div>
                        
                        <div class="dash-grid">
                            <%-- İÇ DÖNGÜ: MAKALELER --%>
                            <asp:Repeater ID="repItems" runat="server" OnItemCommand="repItems_ItemCommand">
                                <ItemTemplate>
                                    <div class="dash-card">
                                        <a href='<%# Eval("Url") %>' target="_blank" class="dash-title">
                                            <%# Eval("Title") %>
                                        </a>
                                        <div class="dash-meta">
                                            <%# Eval("Summary") %>
                                        </div>
                                        <div class="dash-footer">
                                            <asp:LinkButton runat="server" ID="btnAdd" 
                                                CommandName="add" 
                                                CommandArgument='<%# Eval("Title") + "||" + Eval("Url") %>'
                                                CssClass="btn btn-sm btn-outline-primary">
                                                Ekle
                                            </asp:LinkButton>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlNoData" runat="server" Visible="false" CssClass="alert alert-warning">
                İlgi alanlarınıza uygun içerik bulunamadı. <a href="Profile.aspx">Profilim</a> sayfasından ilgi alanı ekleyin.
            </asp:Panel>

        </ContentTemplate>
    </asp:UpdatePanel>

</asp:Content>