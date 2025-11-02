using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class DersProgramiController : Controller
    {
        private readonly AppDbContext _context;

        public DersProgramiController(AppDbContext context)
     {
            _context = context;
        }

    // Haftalýk Ders Programý - TÜM DERSLER VE HOCALAR
        public async Task<IActionResult> Index(int? fakulteId)
        {
  if (!fakulteId.HasValue)
                fakulteId = 1;

  ViewBag.SelectedFakulteId = fakulteId;
            ViewBag.Fakulteler = await _context.Fakulteler.ToListAsync();

            // HocaDers tablosundan tüm ders-hoca eþleþmelerini al
            var hocaDersler = await _context.HocaDersler
       .Include(hd => hd.Hoca)
    .Include(hd => hd.Ders)
    .Where(hd => hd.Hoca.FakulteId == fakulteId && hd.Hoca.AktifMi)
                .ToListAsync();

        // Mevcut ders programý kayýtlarýný al
     var mevcutDersler = await _context.DersProgrami
      .Include(d => d.Hoca)
                .Include(d => d.Ders)
     .Where(d => d.FakulteId == fakulteId)
           .ToListAsync();

            // ViewModel oluþtur
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
      // Bu hoca için bu derse ait ders programý saatlerini bul
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
       // Çakýþma kontrolü
            var mevcutDers = await _context.DersProgrami
       .AnyAsync(d =>
      d.FakulteId == request.FakulteId &&
   d.KisimNo == request.KisimNo &&
    d.DersGunu == request.Gun &&
      d.DersSaati == request.Saat);

             if (mevcutDers)
    {
 return Json(new { success = false, message = $"Kýsým {request.KisimNo} - {request.Gun} {request.Saat}. saat zaten dolu!" });
}

       // Hoca kontrolü
        var hoca = await _context.Hocalar.FindAsync(request.HocaId);
     if (hoca == null)
    {
          return Json(new { success = false, message = "Hoca bulunamadý!" });
        }

         // Ders adýný HocaDers tablosundan al
            var hocaDers = await _context.HocaDersler
      .Include(hd => hd.Ders)
      .FirstOrDefaultAsync(hd => hd.HocaId == request.HocaId && hd.DersId == request.DersId);

  if (hocaDers == null)
                {
       return Json(new { success = false, message = "Hoca-Ders eþleþmesi bulunamadý!" });
    }

              // Yeni ders programý oluþtur
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

  return Json(new { 
       success = true, 
          message = "Ders baþarýyla eklendi!", 
                    dersId = ders.Id, 
kisimNo = ders.KisimNo 
 });
         }
  catch (Exception ex)
     {
                return Json(new { success = false, message = "Hata: " + ex.Message });
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
    TempData["Error"] = "Ders bulunamadý.";
       return RedirectToAction(nameof(Index), new { fakulteId });
     }

            _context.DersProgrami.Remove(ders);
 await _context.SaveChangesAsync();

     TempData["Success"] = "Ders baþarýyla silindi.";
    return RedirectToAction(nameof(Index), new { fakulteId });
        }

        // Toplu Ders Ekleme Sayfasý - GET
   [HttpGet]
        public IActionResult BulkAdd()
        {
          ViewBag.Fakulteler = new SelectList(_context.Fakulteler, "Id", "Ad");
       ViewBag.Gunler = GunSabitler.HaftaGunleri;
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
           TempData["Error"] = "Hiç ders giriþi bulunamadý!";
           ViewBag.Fakulteler = new SelectList(_context.Fakulteler, "Id", "Ad");
    ViewBag.Gunler = GunSabitler.HaftaGunleri;
          return View(model);
            }

            var selectedDersler = model.Dersler.Where(d => d.Selected).ToList();

    if (!selectedDersler.Any())
        {
                TempData["Error"] = "Seçili ders bulunamadý!";
          ViewBag.Fakulteler = new SelectList(_context.Fakulteler, "Id", "Ad");
            ViewBag.Gunler = GunSabitler.HaftaGunleri;
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
                 errors.Add($"Kýsým {item.KisimNo} - {item.Gun} {item.Saat}. saat zaten dolu");
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
           TempData["Success"] = $"? {addedCount} ders baþarýyla eklendi!";
            }

            if (skippedCount > 0)
      {
   TempData["Warning"] = $"?? {skippedCount} ders atlandý: {string.Join(", ", errors)}";
       }

          if (errors.Any() && addedCount == 0)
  {
       TempData["Error"] = $"? Hiç ders eklenemedi: {string.Join(", ", errors)}";
      }

  return RedirectToAction(nameof(Index));
    }

     private async Task<Hoca> FindOrCreateHocaAsync(int fakulteId, string adSoyad)
        {
         var hoca = await _context.Hocalar
   .FirstOrDefaultAsync(h => h.AdSoyad == adSoyad && h.FakulteId == fakulteId);

    if (hoca != null)
          return hoca;

 var parts = adSoyad.Split(new[] { ' ' }, 2);
  string rutbe = parts.Length > 1 ? parts[0] : "";

          hoca = new Hoca
        {
        FakulteId = fakulteId,
                Rutbe = rutbe,
           AdSoyad = adSoyad,
                AktifMi = true
};

            _context.Hocalar.Add(hoca);
        await _context.SaveChangesAsync();

      return hoca;
     }

        // Ders adý autocomplete - AJAX
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

    // Hoca adý autocomplete - Ders bazlý filtreleme ile
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

    // ViewModel
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

    // Yeni ViewModels - Ders Programý için
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
