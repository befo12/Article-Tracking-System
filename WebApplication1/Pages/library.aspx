<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true" CodeBehind="Library.aspx.cs"
    Inherits="WebApplication1.Pages.Library" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <!-- NOT KUTUSU İÇİN STİLLER -->
  <style>
      .note-wrap {
          display: flex;
          align-items: flex-start;
          gap: 6px;
      }

      .note-box {
          width: 100%;
          min-height: 32px;
          font-size: 14px;
          padding: 6px 8px;
          transition: all .2s ease;
          resize: none;
          overflow: hidden;
          border: 1px solid #ccc;
          border-radius: 4px;
      }

      /* Normal — tek satır görünüm */
      .note-box.collapsed {
          height: 32px !important;
          white-space: nowrap;
          text-overflow: ellipsis;
          overflow: hidden;
      }

      /* Odaklanınca açılır */
      .note-box.expanded {
          height: 120px !important;
          white-space: normal;
          overflow-y: auto;
          resize: vertical;
      }
  </style>

  <h2 class="mb-3">Kütüphanem</h2>
  <asp:Label runat="server" ID="lblInfo" CssClass="mb-2 d-block text-muted" />

  <!-- FİLTRE BÖLÜMÜ -->
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

  <!-- GRID -->
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

      <%-- Aç (PDF / HTML / Orijinal) --%>
      <asp:TemplateField HeaderText="Aç">
        <ItemTemplate>

          <!-- PDF varsa -->
          <asp:PlaceHolder ID="phPdf" runat="server"
              Visible='<%# !string.IsNullOrEmpty(Convert.ToString(Eval("PdfPath"))) %>'>
            <a class="btn btn-sm btn-outline-primary me-1"
               target="_blank"
               href='<%# "/SecureFile.ashx?id=" + Eval("Id") %>'>PDF Aç</a>
          </asp:PlaceHolder>

          <!-- HTML varsa -->
          <asp:PlaceHolder ID="phHtml" runat="server"
              Visible='<%# !string.IsNullOrEmpty(Convert.ToString(Eval("HtmlPath"))) %>'>
            <a class="btn btn-sm btn-outline-secondary me-1"
               target="_blank"
               href='<%# "/SecureFile.ashx?id=" + Eval("Id") %>'>HTML Aç</a>
          </asp:PlaceHolder>

          <!-- İkisi de yoksa -->
          <asp:PlaceHolder ID="phNone" runat="server"
              Visible='<%# string.IsNullOrEmpty(Convert.ToString(Eval("PdfPath")))
                      && string.IsNullOrEmpty(Convert.ToString(Eval("HtmlPath"))) %>'>
            <span class="text-muted me-2">Yerel kopya yok</span>
            <a class="btn btn-sm btn-outline-dark"
               target="_blank"
               href='<%# Eval("Url") %>'>Orijinal</a>
          </asp:PlaceHolder>

        </ItemTemplate>
      </asp:TemplateField>

      <%-- Not + Kaydet (expand/collapse) --%>
      <asp:TemplateField HeaderText="Not">
        <ItemTemplate>
          <div class="note-wrap">
            <asp:TextBox ID="txtNote" runat="server"
                         Text='<%# Bind("Note") %>'
                         TextMode="MultiLine"
                         CssClass="form-control form-control-sm note-box collapsed"
                         onfocus="noteFocus(this);"
                         onblur="noteBlur(this);"
                         oninput="noteInput(this);" />
            <asp:LinkButton runat="server"
                            CssClass="btn btn-sm btn-primary"
                            CommandName="saveNote"
                            CommandArgument='<%# Eval("Id") %>'>
              Kaydet
            </asp:LinkButton>
          </div>
        </ItemTemplate>
      </asp:TemplateField>
        <asp:TemplateField HeaderText="Oku">
    <ItemTemplate>
        <asp:HyperLink ID="btnRead" runat="server"
            CssClass="btn btn-sm btn-success"
            NavigateUrl='<%# "~/Pages/Read.aspx?id=" + Eval("Id") %>'>
            Oku
        </asp:HyperLink>
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
          <asp:LinkButton runat="server"
                          CommandName="remove"
                          CommandArgument='<%# Eval("Id") %>'
                          CssClass="btn btn-sm btn-outline-danger">
            Sil
          </asp:LinkButton>
        </ItemTemplate>
      </asp:TemplateField>

    </Columns>

  </asp:GridView>

  <!-- NOT KUTUSU JS (expand / collapse + auto-height) -->
  <script type="text/javascript">
      // textarea yüksekliğini içeriğe göre ayarla
      function noteAutoSize(el) {
          el.style.height = "auto";
          el.style.height = (el.scrollHeight) + "px";
      }

      function noteFocus(el) {
          el.classList.remove("collapsed");
          el.classList.add("expanded");
          noteAutoSize(el);
      }

      function noteBlur(el) {
          // Kaydettikten sonra da collapse olacak, sadece ilk satır görünsün
          el.classList.remove("expanded");
          el.classList.add("collapsed");
      }

      function noteInput(el) {
          noteAutoSize(el);
      }

      // Sayfa yüklenince mevcut not kutularını collapsed moda al
      window.addEventListener("load", function () {
          var areas = document.querySelectorAll(".note-box");
          areas.forEach(function (a) {
              a.classList.add("collapsed");
          });
      });
  </script>

</asp:Content>
