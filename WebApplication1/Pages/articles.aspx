<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true" CodeBehind="Library.aspx.cs"
    Inherits="WebApplication1.Pages.Library" %>

<!-- Sayfaya özel CSS -->
<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        /* Yalnızca Library.aspx içindeki filtre barı için eşit yükseklik */
        .filter-bar .form-control,
        .filter-bar .form-select,
        .filter-bar .btn {
            height: 42px;
        }

        .filter-bar .input-group-text {
            height: 42px;
        }

        @media (min-width: 992px) {
            .filter-bar .col-auto {
                width: auto;
            }
        }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-3">
        <h2 class="mb-3 fw-semibold text-primary">📚 Kütüphanem</h2>

        <!-- Bilgi etiketi -->
        <asp:Label runat="server" ID="lblInfo" CssClass="mb-3 d-block text-muted small" />

        <!-- Filtre kartı -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-body">

                <!-- TEK ve TEMİZ FİLTRE BAR -->
                <div class="filter-bar d-flex flex-wrap align-items-center gap-2">

                    <!-- Arama kutusu -->
                    <div class="col-auto">
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control"
                            placeholder="örn: slice 2mm NIST" />
                    </div>

                    <!-- Nerede ara? (Başlık / Not / İkisi) -->
                    <div class="col-auto">
                        <asp:DropDownList ID="ddlSearchIn" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Başlık" Value="title" />
                            <asp:ListItem Text="Not" Value="note" />
                            <asp:ListItem Text="Başlık + Not" Value="both" Selected="True" />
                        </asp:DropDownList>
                    </div>

                    <!-- Eşleşme (AND / OR) -->
                    <div class="col-auto">
                        <asp:DropDownList ID="ddlMatch" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Tüm kelimeler (AND)" Value="AND" Selected="True" />
                            <asp:ListItem Text="Herhangi biri (OR)" Value="OR" />
                        </asp:DropDownList>
                    </div>

                    <!-- Okundu durumu -->
                    <div class="col-auto">
                        <asp:DropDownList ID="ddlRead" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Hepsi" Value="" />
                            <asp:ListItem Text="Okunmadı" Value="0" />
                            <asp:ListItem Text="Okundu" Value="1" />
                        </asp:DropDownList>
                    </div>

                    <!-- Uygula -->
                    <div class="col-auto">
                        <asp:Button ID="btnSearch" runat="server" Text="Uygula"
                            CssClass="btn btn-primary fw-semibold"
                            OnClick="btnSearch_Click" />
                    </div>

                </div>
                <!-- /filter-bar -->

            </div>
        </div>

        <!-- Grid -->
        <asp:GridView runat="server" ID="gvLib" AutoGenerateColumns="False"
            CssClass="table table-hover align-middle"
            DataKeyNames="Id"
            OnRowCommand="gvLib_RowCommand"
            OnPageIndexChanging="gvLib_PageIndexChanging"
            AllowPaging="true" PageSize="10">

            <Columns>
                <%-- Başlık --%>
                <asp:BoundField DataField="Title" HeaderText="Başlık" />

                <%-- Bağlantı --%>
                <asp:HyperLinkField DataNavigateUrlFields="Url" DataTextField="Url" HeaderText="Bağlantı" />

                <%-- Not --%>
                <asp:TemplateField HeaderText="Not">
                    <ItemTemplate>
                        <div class="d-flex gap-2">
                            <asp:TextBox ID="txtNote" runat="server" CssClass="form-control form-control-sm"
                                Text='<%# Bind("Note") %>' />
                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-success"
                                CommandName="saveNote"
                                CommandArgument='<%# Eval("Id") %>'>💾</asp:LinkButton>
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

                <%-- Silme --%>
                <asp:TemplateField HeaderText="İşlem">
                    <ItemTemplate>
                        <asp:LinkButton runat="server" CommandName="remove"
                            CommandArgument='<%# Eval("Id") %>'
                            CssClass="btn btn-sm btn-outline-danger">🗑</asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>

    </div>
</asp:Content>
