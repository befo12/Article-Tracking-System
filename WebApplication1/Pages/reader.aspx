<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="reader.aspx.cs" Inherits="WebApplication1.Pages.ReaderPage" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Okuma Modu</title>
    <style>
        html,body{height:100%;margin:0;padding:0;}
        .topbar{padding:10px;border-bottom:1px solid #ddd;display:flex;gap:10px;align-items:center;}
        .title{font-weight:600;flex:1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
        iframe{width:100%;height:calc(100% - 52px);border:0;}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="topbar">
            <span class="title"><asp:Literal ID="litTitle" runat="server" /></span>
            <asp:HyperLink ID="lnkOpen" runat="server" Text="Orijinali Aç" Target="_blank" />
            <asp:HyperLink ID="lnkDownload" runat="server" Text="İndir" Target="_blank" />
            <asp:Button ID="btnToggleRead" runat="server" Text="Okundu Yap" OnClick="btnToggleRead_Click" />
        </div>
        <iframe id="frame" runat="server"></iframe>
    </form>
</body>
</html>
