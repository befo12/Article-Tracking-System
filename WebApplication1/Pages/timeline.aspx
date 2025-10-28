<%@ Page Language="C#" %>
<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="utf-8" />
  <title>Main – Topluluk Akışı</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" />
  <style>
    body { background-color:#f8f9fa; }
    .card-soft{border-radius:1rem;box-shadow:0 8px 28px rgba(0,0,0,.06)}
    .badge-topic{background:#eef;border:1px solid #dde;color:#334;}
  </style>
</head>
<body class="container py-5">

  <h2 class="mb-4 text-center">💬 Main – Topluluk Akışı</h2>

  <div class="vstack gap-3">
    <div class="card card-soft p-3">
      <div class="d-flex justify-content-between">
        <div class="fw-semibold">“Okuma Modu” tasarımı üzerine</div>
        <span class="badge badge-topic">UI/UX</span>
      </div>
      <div class="small text-secondary mb-2">by <b>Demo</b> • 2 saat önce</div>
      <div class="mb-2">
        Okuma modunda satır yüksekliği ve kenar boşlukları nasıl olmalı? Önerileriniz?
        <span class="text-muted">(#12 makalesine atıf)</span>
      </div>
      <div class="small text-secondary">12 yanıt • 134 görüntüleme</div>
    </div>

    <div class="card card-soft p-3">
      <div class="d-flex justify-content-between">
        <div class="fw-semibold">Grafen süperkapasitör notları</div>
        <span class="badge badge-topic">Materials</span>
      </div>
      <div class="small text-secondary mb-2">by <b>Ada</b> • dün</div>
      <div class="mb-2">Hızlı şarj/deşarj üzerine kaynaklar ekledim. PDF linklerini paylaşıyorum.</div>
      <div class="small text-secondary">7 yanıt • 89 görüntüleme</div>
    </div>

    <div class="card card-soft p-3">
      <div class="d-flex justify-content-between">
        <div class="fw-semibold">Geant4 doz simülasyonu</div>
        <span class="badge badge-topic">Physics</span>
      </div>
      <div class="small text-secondary mb-2">by <b>Kenan</b> • 2 gün önce</div>
      <div class="mb-2">Monte Carlo parametreleri ve referans setleri üzerine kısa derleme.</div>
      <div class="small text-secondary">3 yanıt • 45 görüntüleme</div>
    </div>
  </div>

  <div class="alert alert-info mt-4">
    Bu alan şu an <b>okunur</b>. Giriş yaptıktan sonra tartışma başlatma/yanıt verme butonları eklenecek.
  </div>

  <div class="text-center mt-3">
    <a href="login.aspx" class="btn btn-primary">Giriş Yap</a>
    <a href="hub.aspx" class="btn btn-link">← Merkez</a>
  </div>

</body>
</html>
