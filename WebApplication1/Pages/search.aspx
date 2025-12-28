<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Search.aspx.cs"
    Inherits="WebApplication1.Pages.Search" MasterPageFile="~/Master/Site.Master" %>

<asp:Content ID="t" ContentPlaceHolderID="TitleContent" runat="server">
  Makale Arama
</asp:Content>

<asp:Content ID="m" ContentPlaceHolderID="MainContent" runat="server">


  <style>
    .card-soft { border-radius:1rem; box-shadow:0 .25rem 1rem rgba(0,0,0,.06); }
    .muted { color:#6c757d }
    .result-row.inlib { background:#fff9e6; }
  </style>

  <asp:UpdatePanel ID="upd" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

      <h3 class="mb-1">Makale Arama</h3>
      <asp:Label ID="lblInfo" runat="server" CssClass="d-block mb-3 muted" EnableViewState="false" />

      <div class="row">

        <!-- SOL PANEL -->
        <div class="col-lg-3">
          <div class="card card-soft p-3 mb-3">

            <label class="form-label">Anahtar kelime</label>
            <asp:TextBox ID="txtQuery" runat="server" CssClass="form-control" placeholder="ör. deep learning" />

            <div class="mt-3">
              <label class="form-label">Sıralama</label>
              <asp:DropDownList ID="ddlSort" runat="server" CssClass="form-select"
                  AutoPostBack="true" OnSelectedIndexChanged="FilterChanged">
                <asp:ListItem Text="Alaka (varsayılan)" Value="relevance" />
                <asp:ListItem Text="Yıl (yeni → eski)" Value="date_desc" />
                <asp:ListItem Text="Yıl (eski → yeni)" Value="date_asc" />
                <asp:ListItem Text="Başlık (A→Z)" Value="title_asc" />
              </asp:DropDownList>
            </div>

            <hr/>

            <label class="form-label">Tam ifade</label>
            <asp:TextBox ID="txtExact" runat="server" CssClass="form-control"
                AutoPostBack="true" OnTextChanged="FilterChanged"
                placeholder='örn. "federated learning"' />

            <div class="mt-3">
              <label class="form-label">Yazar içerir</label>
              <asp:TextBox ID="txtAuthor" runat="server" CssClass="form-control"
                  AutoPostBack="true" OnTextChanged="FilterChanged" />
            </div>

            <div class="mt-3">
              <label class="form-label">Dergi/konferans içerir</label>
              <asp:TextBox ID="txtVenue" runat="server" CssClass="form-control"
                  AutoPostBack="true" OnTextChanged="FilterChanged" />
            </div>

            <!-- DİL FİLTRESİ -->
            <div class="mt-3">
              <label class="form-label">Dil</label>
              <asp:DropDownList ID="ddlLang" runat="server" CssClass="form-select"
                  AutoPostBack="true" OnSelectedIndexChanged="FilterChanged">
                  <asp:ListItem Text="Tümü" Value="" />
                  <asp:ListItem Text="İngilizce" Value="en" />
                  <asp:ListItem Text="Türkçe" Value="tr" />
                  <asp:ListItem Text="Almanca" Value="de" />
                  <asp:ListItem Text="Fransızca" Value="fr" />
                  <asp:ListItem Text="İspanyolca" Value="es" />
              </asp:DropDownList>
            </div>

            <div class="mt-3">
              <label class="form-label">Yıl aralığı</label>
              <div class="input-group">
                <asp:TextBox ID="txtYearFrom" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnTextChanged="FilterChanged" placeholder="2019" />
                <span class="input-group-text">–</span>
                <asp:TextBox ID="txtYearTo" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnTextChanged="FilterChanged" placeholder="2025" />
              </div>
            </div>

            <div class="form-check mt-3">
              <asp:CheckBox ID="chkExactPhrase" runat="server" CssClass="form-check-input"
                  AutoPostBack="true" OnCheckedChanged="FilterChanged" />
              <label class="form-check-label">Başlıkta tam ifade</label>
            </div>

            <div class="form-check mt-2">
              <asp:CheckBox ID="chkHasPdf" runat="server" CssClass="form-check-input"
                  AutoPostBack="true" OnCheckedChanged="FilterChanged" />
              <label class="form-check-label">Sadece PDF’i olanlar</label>
            </div>

            <div class="form-check mt-2">
              <asp:CheckBox ID="chkInLibOnly" runat="server" CssClass="form-check-input"
                  AutoPostBack="true" OnCheckedChanged="FilterChanged" />
              <label class="form-check-label">Sadece kütüphanemde olanlar</label>
            </div>

            <div class="d-grid gap-2 mt-3">
              <asp:Button ID="btnSearch" runat="server" CssClass="btn btn-primary"
                  Text="Ara" OnClick="btnSearch_Click" />
            </div>

          </div>
        </div>

        <!-- SONUÇLAR -->
        <div class="col-lg-9">

          <asp:HiddenField ID="hfCursor" runat="server" />

          <div class="d-flex justify-content-between align-items-center mb-2">
            <span class="muted">İlk yükleme 50; “Daha Fazla” 50’şer ekler.</span>
            <asp:Button ID="btnLoadMore" runat="server" CssClass="btn btn-outline-secondary btn-sm"
                        Text="Daha Fazla" OnClick="btnLoadMore_Click" />
          </div>

          <asp:GridView ID="gvResults" runat="server" CssClass="table table-striped"
              AutoGenerateColumns="false"
              DataKeyNames="Url,Title,InLibrary,Doi"
              AllowPaging="true" PageSize="20"
              PagerSettings-Mode="Numeric" PagerSettings-Position="Bottom"
              OnPageIndexChanging="gvResults_PageIndexChanging"
              OnRowDataBound="gvResults_RowDataBound"
              OnRowCommand="gvResults_RowCommand">

            <Columns>

              <asp:TemplateField HeaderText="Başlık">
                <ItemTemplate>
                  <span><%# Eval("Title") %></span>
                </ItemTemplate>
              </asp:TemplateField>

              <asp:BoundField DataField="Authors" HeaderText="Yazarlar" />
              <asp:BoundField DataField="Venue" HeaderText="Dergi/Konferans" />
              <asp:BoundField DataField="Year" HeaderText="Yıl" />

              <asp:HyperLinkField HeaderText="Bağlantı"
                  DataNavigateUrlFields="Url" DataTextField="Doi" Target="_blank" />

              <asp:TemplateField HeaderText="İşlem">
                <ItemTemplate>

                  <asp:LinkButton ID="btnAdd" runat="server"
                      CssClass="btn btn-outline-primary btn-sm"
                      Text="Kütüphaneme Ekle"
                      CommandName="AddToLib"
                      CommandArgument="<%# ((GridViewRow)Container).RowIndex %>"
                      OnClientClick="showLoading();" />

                  <asp:LinkButton ID="btnRemove" runat="server"
                      CssClass="btn btn-outline-danger btn-sm"
                      Text="Kütüphaneden Çıkar"
                      CommandName="RemoveFromLib"
                      CommandArgument="<%# ((GridViewRow)Container).RowIndex %>"
                      OnClientClick="showLoading();" />

                </ItemTemplate>
              </asp:TemplateField>

            </Columns>

          </asp:GridView>

        </div>
      </div>

    </ContentTemplate>
  </asp:UpdatePanel>

</asp:Content>
