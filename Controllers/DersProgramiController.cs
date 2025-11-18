using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace Etutlist.Controllers
{
    public class DersProgramiController : Controller
    {
        private readonly AppDbContext _context;

    // ? SAAT AYARLARI - Değiştirilebilir
        private static readonly Dictionary<int, int[]> FakulteSaatAyarlari = new()
        {
            // ASEM (FakulteId: 2): Pazartesi:7, Salı:7, Çarşamba:6, Perşembe:7, Cuma:4
            { 2, new[] { 7, 7, 6, 7, 4 } },
    // SUEM (FakulteId: 1): Pazartesi:6, Salı:6, Çarşamba:6, Perşembe:7, Cuma:4
          { 1, new[] { 6, 6, 6, 7, 4 } }
        };

  // CSV Sütun başlangıç pozisyonları (1. sütun: S.NO, 2: DERS, 3: HOCA, 4: D/S)
  private const int CSV_ILKSAAT_SUTUN = 4;

  public DersProgramiController(AppDbContext context)
        {
            _context = context;
     }

   // Haftalık Ders Programı - TÜM DERSLER VE HOCALAR
      public async Task<IActionResult> Index(int? fakulteId)
     {
          if (!fakulteId.HasValue)
     fakulteId = 1;

 ViewBag.SelectedFakulteId = fakulteId;
            ViewBag.Fakulteler = await _context.Fakulteler.ToListAsync();

 var hocaDersler = await _context.HocaDersler
        .Include(hd => hd.Hoca)
 .Include(hd => hd.Ders)
          .Where(hd => hd.Hoca.FakulteId == fakulteId && hd.Hoca.AktifMi)
      .ToListAsync();

        var mevcutDersler = await _context.DersProgrami
      .Include(d => d.Hoca)
         .Include(d => d.Ders)
        .Where(d => d.FakulteId == fakulteId)
                .ToListAsync();

     var dersGruplari = hocaDersler
          .GroupBy(hd => hd.Ders.DersAdi)
             .Select(g => new DersGrupViewModel
    {
            DersAdi = g.Key,
             DersId = g.First().DersId,
 Hocalar = g.Select(hd => new HocaDersViewModel
   {
 HocaId = hd.HocaId,
              HocaAdi = hd.Hoca.AdSoyad,
   DersAdi = hd.Ders.DersAdi,
        DersSaatleri = mevcutDersler
        .Where(d => d.HocaId == hd.HocaId && d.DersAdi == hd.Ders.DersAdi)
     .ToList()
           }).ToList()
          }).ToList();

    return View(dersGruplari);
        }

        // Ders ekle - AJAX POST
        [HttpPost]
        public async Task<IActionResult> AddDers([FromBody] AddDersRequest request)
        {
            try
     {
        var mevcutDers = await _context.DersProgrami.AnyAsync(d =>
       d.FakulteId == request.FakulteId &&
           d.KisimNo == request.KisimNo &&
           d.DersGunu == request.Gun &&
        d.DersSaati == request.Saat);

      if (mevcutDers)
      return Json(new { success = false, message = $"Kısım {request.KisimNo} - {request.Gun} {request.Saat}. saat zaten dolu!" });

           var hoca = await _context.Hocalar.FindAsync(request.HocaId);
       if (hoca == null)
        return Json(new { success = false, message = "Hoca bulunamadı!" });

 var hocaDers = await _context.HocaDersler
.Include(hd => hd.Ders)
     .FirstOrDefaultAsync(hd => hd.HocaId == request.HocaId && hd.DersId == request.DersId);

        if (hocaDers == null)
     return Json(new { success = false, message = "Hoca-Ders eşleşmesi bulunamadı!" });

        var ders = new DersProgrami
  {
     FakulteId = request.FakulteId,
KisimNo = request.KisimNo,
             HocaId = request.HocaId,
      DersId = request.DersId,
        DersAdi = hocaDers.Ders.DersAdi,
        DersKodu = "",
          DersGunu = request.Gun,
           DersSaati = request.Saat
       };

            _context.DersProgrami.Add(ders);
                await _context.SaveChangesAsync();

          return Json(new
      {
   success = true,
        message = "Ders başarıyla eklendi!",
    dersId = ders.Id,
             kisimNo = ders.KisimNo
     });
            }
            catch (Exception ex)
            {
           return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

    // Toplu Ders Ekleme Sayfası - GET
        [HttpGet]
        public async Task<IActionResult> BulkAdd()
  {
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
      ViewBag.Gunler = new List<string> { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };
            return View();
        }

        // Toplu Ders Ekleme - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAdd(BulkAddViewModel model)
        {
        int addedCount = 0;
    int skippedCount = 0;
            var errors = new List<string>();

     if (model.Dersler == null || !model.Dersler.Any())
            {
           TempData["Error"] = "Hiç ders girişi bulunamadı!";
 ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
                ViewBag.Gunler = new List<string> { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };
    return View(model);
       }

        var selectedDersler = model.Dersler.Where(d => d.Selected).ToList();

  if (!selectedDersler.Any())
        {
      TempData["Error"] = "Seçili ders bulunamadı!";
        ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
    ViewBag.Gunler = new List<string> { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };
          return View(model);
        }

        foreach (var item in selectedDersler)
       {
                try
           {
  var mevcutDers = await _context.DersProgrami
       .AnyAsync(d =>
       d.FakulteId == model.FakulteId &&
     d.KisimNo == item.KisimNo &&
     d.DersGunu == item.Gun &&
     d.DersSaati == item.Saat);

 if (mevcutDers)
        {
           skippedCount++;
                errors.Add($"Kısım {item.KisimNo} - {item.Gun} {item.Saat}. saat zaten dolu");
    continue;
        }

              var hoca = await FindOrCreateHocaAsync(model.FakulteId, item.HocaAdi);

      var ders = new DersProgrami
        {
     FakulteId = model.FakulteId,
  KisimNo = item.KisimNo,
      HocaId = hoca.Id,
     DersAdi = item.DersAdi,
   DersKodu = item.DersKodu ?? "",
       DersId = model.DersId,
             DersGunu = item.Gun,
            DersSaati = item.Saat
 };

          _context.DersProgrami.Add(ders);
       addedCount++;
     }
       catch (Exception ex)
    {
         errors.Add($"{item.DersAdi} - {item.Gun} {item.Saat}. saat: {ex.Message}");
            }
       }

      if (addedCount > 0)
            {
       await _context.SaveChangesAsync();
    TempData["Success"] = $"? {addedCount} ders başarıyla eklendi!";
     }

    if (skippedCount > 0)
{
     TempData["Warning"] = $"?? {skippedCount} ders atlandı: {string.Join(", ", errors)}";
  }

            if (errors.Any() && addedCount == 0)
         {
   TempData["Error"] = $"? Hiç ders eklenemedi: {string.Join(", ", errors)}";
            }

        return RedirectToAction(nameof(Index));
   }

      // Excel/CSV Upload Sayfası - GET
        [HttpGet]
        public async Task<IActionResult> UploadExcel()
   {
    ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
      return View();
        }

        // Excel/CSV Upload - POST
  [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(int fakulteId, IFormFile file)
 {
       if (file == null || file.Length == 0)
          {
     TempData["Error"] = "Lütfen bir dosya seçin!";
          ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
       return View();
         }

     try
            {
              int addedCount = 0;
             int skippedCount = 0;
      int hocaCount = 0;
        int dersCount = 0;

    // Fakülteye özel saat ayarlarını al
    var saatAyarlari = FakulteSaatAyarlari.ContainsKey(fakulteId)
       ? FakulteSaatAyarlari[fakulteId]
             : new[] { 9, 9, 9, 9, 9 }; // Default: Her gün 9 saat

          using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
        {
         string line;
  int lineNumber = 0;
             string currentDersAdi = "";

    var gunler = new[] { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };

    while ((line = await reader.ReadLineAsync()) != null)
        {
  lineNumber++;

    if (lineNumber <= 2) continue;

            var columns = line.Split(';');
     if (columns.Length < 4) continue;

    if (string.IsNullOrWhiteSpace(columns[0]) && string.IsNullOrWhiteSpace(columns[1])) continue;

if (!string.IsNullOrWhiteSpace(columns[1]))
    {
       currentDersAdi = columns[1].Trim();

  var dersKaydi = await _context.Dersler.FirstOrDefaultAsync(d => d.DersAdi == currentDersAdi);
         if (dersKaydi == null)
       {
     dersKaydi = new Ders { DersAdi = currentDersAdi };
       _context.Dersler.Add(dersKaydi);
      await _context.SaveChangesAsync();
    dersCount++;
  }
 }

    if (string.IsNullOrWhiteSpace(currentDersAdi)) continue;

      var hocaBilgi = columns[2]?.Trim();
      if (string.IsNullOrWhiteSpace(hocaBilgi)) continue;

          var hoca = await FindOrCreateHocaAsync(fakulteId, hocaBilgi);
       if (hoca == null) continue;

           var bulunanDers = await _context.Dersler.FirstOrDefaultAsync(d => d.DersAdi == currentDersAdi);
             if (bulunanDers == null) continue;

              var hocaDers = await _context.HocaDersler
 .FirstOrDefaultAsync(hd => hd.HocaId == hoca.Id && hd.DersId == bulunanDers.Id);
       if (hocaDers == null)
        {
      hocaDers = new HocaDers
    {
   HocaId = hoca.Id,
  DersId = bulunanDers.Id
            };
       _context.HocaDersler.Add(hocaDers);
         await _context.SaveChangesAsync();
      hocaCount++;
    }

         // Dinamik sütun başlangıç hesaplama
int currentColumn = CSV_ILKSAAT_SUTUN;

           for (int gunIndex = 0; gunIndex < 5; gunIndex++)
       {
  var gun = gunler[gunIndex];
      int maxSaat = saatAyarlari[gunIndex];

            for (int saat = 1; saat <= maxSaat; saat++)
       {
         if (currentColumn >= columns.Length) break;

  var kisimNo = columns[currentColumn]?.Trim();
    currentColumn++;

                if (string.IsNullOrWhiteSpace(kisimNo)) continue;

   // ? DÜZELTME: Birden fazla kısım desteği (1-2, 1-2-3 gibi)
      var kisimlar = new List<int>();

 if (kisimNo.Contains('-'))
            {
 // "1-2" veya "1-2-3" formatı
  var parcalar = kisimNo.Split('-');
      foreach (var parca in parcalar)
  {
        if (int.TryParse(parca.Trim(), out int k))
   kisimlar.Add(k);
  }
      }
       else if (int.TryParse(kisimNo, out int tekKisim))
  {
      // Tek kısım: "1", "2" gibi
          kisimlar.Add(tekKisim);
       }

       if (!kisimlar.Any()) continue;

           // Her kısım için ayrı ders programı kaydı oluştur
        foreach (var kisim in kisimlar)
    {
            var mevcutDers = await _context.DersProgrami.AnyAsync(d =>
           d.FakulteId == fakulteId &&
     d.KisimNo == kisim &&
     d.DersGunu == gun &&
          d.DersSaati == saat);

 if (mevcutDers)
{
       skippedCount++;
    continue;
         }

       var dersProgrami = new DersProgrami
         {
               FakulteId = fakulteId,
    HocaId = hoca.Id,
     DersId = bulunanDers.Id,
       DersAdi = currentDersAdi,
               KisimNo = kisim,
              DersGunu = gun,
      DersSaati = saat,
      DersKodu = ""
  };

      _context.DersProgrami.Add(dersProgrami);
   addedCount++;
    }
            }
       }
          }
           }

    await _context.SaveChangesAsync();

         TempData["Success"] = $"? Başarılı! {addedCount} ders saati, {dersCount} ders ve {hocaCount} hoca-ders eşleşmesi eklendi!";
                if (skippedCount > 0)
    {
        TempData["Warning"] = $"?? {skippedCount} çakışan kayıt atlandı.";
            }

                return RedirectToAction(nameof(Index), new { fakulteId });
 }
            catch (Exception ex)
        {
    TempData["Error"] = "Hata: " + ex.Message;
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
          return View();
            }
        }

      // Ders sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DersSil(int id, int? fakulteId)
        {
    var ders = await _context.DersProgrami.FindAsync(id);
          if (ders == null)
            {
    TempData["Error"] = "Ders bulunamadı.";
   return RedirectToAction(nameof(Index), new { fakulteId });
     }

            _context.DersProgrami.Remove(ders);
       await _context.SaveChangesAsync();

            TempData["Success"] = "Ders başarıyla silindi.";
            return RedirectToAction(nameof(Index), new { fakulteId });
        }

   // Ders adı autocomplete - AJAX
        [HttpGet]
        public IActionResult SearchDersler(string term)
  {
      var dersler = _context.Dersler
                .Where(d => string.IsNullOrEmpty(term) || d.DersAdi.Contains(term))
   .Select(d => new { id = d.Id, dersAdi = d.DersAdi })
     .Take(20)
     .ToList();

       return Json(dersler);
        }

        // Hoca adı autocomplete - Ders bazlı filtreleme ile
        [HttpGet]
        public IActionResult SearchHocalar(int fakulteId, string term, int? dersId)
        {
       var query = _context.Hocalar
          .Where(h => h.AktifMi && h.FakulteId == fakulteId && h.AdSoyad.Contains(term));

            if (dersId.HasValue && dersId > 0)
          {
       query = query.Where(h => h.HocaDersler.Any(hd => hd.DersId == dersId));
          }

            var hocalar = query
       .Select(h => new { id = h.Id, adSoyad = h.AdSoyad, rutbe = h.Rutbe })
                .Take(10)
      .ToList();

   return Json(hocalar);
        }

        private async Task<Hoca> FindOrCreateHocaAsync(int fakulteId, string adSoyad)
        {
  var hoca = await _context.Hocalar
       .FirstOrDefaultAsync(h => h.AdSoyad == adSoyad && h.FakulteId == fakulteId);

 if (hoca != null)
       return hoca;

    // Gelişmiş Rütbe/İsim Ayırma
   // Örnek: "J.Kd.Bçvş.Kenan Akın" ? Rütbe: "J.Kd.Bçvş.", İsim: "Kenan Akın"
      string rutbe = "";
        string isim = adSoyad;

            // Nokta içeren kısaltmaları bul (J.Kd.Bçvş. gibi)
            var match = Regex.Match(adSoyad, @"^([A-ZÇĞİÖŞÜ][a-zçğıöşü]*\.)+");
        if (match.Success)
   {
          rutbe = match.Value.TrimEnd();
        isim = adSoyad.Substring(match.Length).Trim();
            }
            else
            {
                // Boşlukla ayrılmışsa ilk kelime rütbe
        var parts = adSoyad.Split(new[] { ' ' }, 2);
   if (parts.Length > 1)
  {
 rutbe = parts[0];
           isim = parts[1];
       }
       }

 hoca = new Hoca
            {
                FakulteId = fakulteId,
      Rutbe = rutbe,
      AdSoyad = isim,
      AktifMi = true
            };

  _context.Hocalar.Add(hoca);
await _context.SaveChangesAsync();

       return hoca;
        }
    }

    // Request Model
  public class AddDersRequest
    {
        public int FakulteId { get; set; }
   public int HocaId { get; set; }
public int DersId { get; set; }
        public int KisimNo { get; set; }
        public string Gun { get; set; }
    public int Saat { get; set; }
    }

    // BulkAdd ViewModels
    public class BulkAddViewModel
{
        public int FakulteId { get; set; }
        public int? DersId { get; set; }
        public List<DersItemViewModel> Dersler { get; set; } = new();
    }

    public class DersItemViewModel
    {
      public bool Selected { get; set; }
        public string DersKodu { get; set; }
   public string DersAdi { get; set; }
        public int KisimNo { get; set; }
        public string HocaAdi { get; set; }
        public string Gun { get; set; }
        public int Saat { get; set; }
    }

    // Index ViewModels
    public class DersGrupViewModel
    {
        public string DersAdi { get; set; }
        public int DersId { get; set; }
public List<HocaDersViewModel> Hocalar { get; set; }
    }

    public class HocaDersViewModel
    {
    public int HocaId { get; set; }
        public string HocaAdi { get; set; }
        public string DersAdi { get; set; }
        public List<DersProgrami> DersSaatleri { get; set; }
    }
}
