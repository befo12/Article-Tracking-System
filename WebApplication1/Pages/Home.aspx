<%@ Page Language="C#" MasterPageFile="~/Master/Site.Master"
    AutoEventWireup="true" CodeBehind="Home.aspx.cs"
    Inherits="WebApplication1.Pages.Home" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <%-- CSS STİLLERİ --%>
  <style>
      .weekly-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
          gap: 1.5rem;
          margin-top: 1rem;
      }
      .weekly-card {
          background: #fff;
          border: 1px solid #e0e0e0;
          border-radius: 12px;
          padding: 1.25rem;
          position: relative;
          transition: transform 0.2s, box-shadow 0.2s;
          display: flex;
          flex-direction: column;
          justify-content: space-between;
          height: 100%;
      }
      .weekly-card:hover {
          transform: translateY(-5px);
          box-shadow: 0 10px 20px rgba(0,0,0,0.08);
          border-color: #0d6efd;
      }
      .weekly-card::before {
          content: ''; position: absolute; left: 0; top: 15px; bottom: 15px;
          width: 4px; background: #0d6efd;
          border-top-right-radius: 4px; border-bottom-right-radius: 4px;
      }
      .card-meta { font-size: 0.85rem; color: #6c757d; margin-bottom: 0.5rem; font-weight: 600; text-transform: uppercase; }
      .card-title { font-size: 1.1rem; font-weight: 700; color: #2c3e50; text-decoration: none; line-height: 1.4; margin-bottom: 1rem; display: block; }
      .card-title:hover { color: #0d6efd; }
      .card-footer-custom { margin-top: auto; border-top: 1px solid #f0f0f0; padding-top: 0.75rem; display:flex; justify-content:space-between; align-items:center; }
      .author-text { font-size: 0.85rem; color: #999; }
  </style>

  <%-- HOŞ GELDİN ALANI --%>
  <div class="d-flex align-items-center justify-content-between mb-4">
      <div>
          <h2 class="fw-bold text-dark m-0">Hoş geldin, <%: Name %>! 👋</h2>
          <div class="text-muted"><%: Email %></div>
      </div>
      <div><a href="Library.aspx" class="btn btn-outline-primary">📚 Kütüphaneme Git</a></div>
  </div>

  <%-- HIZLI EKLE KUTUSU --%>
  <div class="card shadow-sm border-0 mb-5" style="background: linear-gradient(to right, #f8f9fa, #ffffff);">
    <div class="card-body p-4">
      <h5 class="card-title fw-bold text-primary mb-3">⚡ Hızlı Makale Ekle</h5>
      
      <div class="row g-2 align-items-center">
        <div class="col-md-4">
            <asp:TextBox runat="server" ID="txtQuickTitle" CssClass="form-control" placeholder="Makale Başlığı" />
        </div>
        <div class="col-md-5">
            <asp:TextBox runat="server" ID="txtQuickUrl" CssClass="form-control" placeholder="Link (URL)" />
        </div>
        <div class="col-md-3 text-end">
          <asp:Button runat="server" ID="btnQuickAdd" CssClass="btn btn-primary w-100 fw-semibold" Text="Kaydet" OnClick="btnQuickAdd_Click" />
        </div>
      </div>

      <%-- NOT KUTUSU (Hata buradaydı, şimdi eklendi) --%>
      <div class="mt-2">
        <asp:TextBox runat="server" ID="txtQuickNote" CssClass="form-control" TextMode="MultiLine"
                     placeholder="Kısa bir not ekle (isteğe bağlı)" Rows="1" />
      </div>
      
      <div class="mt-2"><asp:Label runat="server" ID="lblQuickMsg" CssClass="small fw-bold" /></div>
    </div>
  </div>

  <%-- HAFTALIK ÖNERİLER --%>
  <div class="mb-5">
      <div class="d-flex align-items-center mb-3">
          <h4 class="fw-bold m-0 me-2">📅 <asp:Literal ID="litTopic" runat="server" /></h4>
      </div>
      
      <asp:UpdatePanel runat="server" ID="updWeekly">
          <ContentTemplate>
              <div class="weekly-grid">
                <asp:Repeater runat="server" ID="rptWeekly" OnItemCommand="rptWeekly_ItemCommand">
                  <ItemTemplate>
                    <div class="weekly-card">
                        <div class="card-meta"><%# Eval("Year") %> • <%# Eval("Venue") %></div>
                        <a href='<%# Eval("Url") %>' target="_blank" class="card-title"><%# Eval("Title") %></a>
                        
                        <div class="card-footer-custom">
                            <span class="author-text"><i class="bi bi-people-fill"></i> <%# Eval("Authors") %></span>
                            <asp:LinkButton runat="server" ID="btnAdd" 
                                CommandName="Add" 
                                CommandArgument='<%# Eval("Title") + "||" + Eval("Url") %>'
                                CssClass="btn btn-sm btn-outline-primary rounded-pill px-3">
                                <i class="bi bi-plus-lg"></i> Ekle
                            </asp:LinkButton>
                        </div>
                    </div>
                  </ItemTemplate>
                </asp:Repeater>
              </div>
          </ContentTemplate>
      </asp:UpdatePanel>
  </div>

  <%-- SON AKTİVİTELER (Hata buradaydı, şimdi eklendi) --%>
  <h5 class="fw-bold text-secondary border-bottom pb-2 mb-3">Son Aktivitelerim</h5>
  <div class="list-group list-group-flush">
    <asp:Repeater runat="server" ID="rptFeed">
      <ItemTemplate>
        <div class="list-group-item px-0 py-3">
          <div class="d-flex justify-content-between align-items-center">
             <div>
                 <div class="fw-semibold text-dark"><%# Eval("Title") %></div>
                 <div class="small text-muted"><%# Eval("Text") %></div>
             </div>
             <div class="small text-secondary"><%# String.Format("{0:dd.MM.yyyy HH:mm}", Eval("When")) %></div>
          </div>
        </div>
      </ItemTemplate>
    </asp:Repeater>
  </div>

</asp:Content>