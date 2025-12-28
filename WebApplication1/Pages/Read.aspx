<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true" CodeBehind="Read.aspx.cs"
    Inherits="WebApplication1.Pages.Read" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

<link href="/Styles/read.css" rel="stylesheet" />

<!-- DARK MODE TOGGLE -->
<div class="d-flex justify-content-end mb-3">
    <button id="themeToggle" class="btn btn-sm btn-outline-secondary">🌙 Dark Mode</button>
</div>

<div id="readerContainer">

    <!-- SOL PANEL: PDF / HTML -->
    <div id="leftPane">

        <asp:PlaceHolder ID="phPdf" runat="server" Visible="false">
            <iframe id="pdfFrame" runat="server" class="reader-frame"></iframe>
        </asp:PlaceHolder>

        <asp:PlaceHolder ID="phHtml" runat="server" Visible="false">
            <iframe id="htmlFrame" runat="server" class="reader-frame"></iframe>
        </asp:PlaceHolder>

        <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
            <div class="no-file">
                <p>PDF / HTML bulunamadı.</p>
                <asp:HyperLink ID="lnkOriginal" runat="server"
                               CssClass="btn btn-dark" Target="_blank">
                    Orijinal Bağlantı
                </asp:HyperLink>
            </div>
        </asp:PlaceHolder>

    </div>

    <!-- SÜRÜKLEME ÇUBUĞU -->
    <div id="splitter"></div>

    <!-- SAĞ PANEL (NOT + OKUNDU) -->
    <div id="rightPane">

        <h4 class="reader-title">
            <asp:Label ID="lblTitle" runat="server" />
        </h4>

        <label class="form-label">Notlarım</label>
        <asp:TextBox ID="txtNote" runat="server"
                     TextMode="MultiLine"
                     CssClass="note-editor"></asp:TextBox>

        <asp:Button ID="btnSaveNote" runat="server"
                    CssClass="btn btn-primary mt-3"
                    Text="Kaydet" OnClick="btnSaveNote_Click" />

        <div class="form-check mt-3">
            <asp:CheckBox ID="chkRead" runat="server" CssClass="form-check-input"
                          AutoPostBack="true" OnCheckedChanged="chkRead_CheckedChanged" />
            <label class="form-check-label">Okundu olarak işaretle</label>
        </div>

        <asp:Label ID="lblStatus" runat="server"
                   CssClass="text-success mt-2 d-block"></asp:Label>

    </div>
</div>

<script src="/Scripts/read.js"></script>

</asp:Content>
