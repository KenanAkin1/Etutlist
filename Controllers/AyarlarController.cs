using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class AyarlarController : Controller
    {
        private readonly AppDbContext _context;

        public AyarlarController(AppDbContext context)
   {
    _context = context;
        }

        // Ana Ayarlar Sayfasý
 public async Task<IActionResult> Index()
        {
            var model = new AyarlarViewModel
            {
       Fakulteler = await _context.Fakulteler.ToListAsync(),
      Hocalar = await _context.Hocalar
  .Include(h => h.Fakulte)
   .Include(h => h.Dersler)
          .Include(h => h.HocaDersler)
      .ThenInclude(hd => hd.Ders)
        .ToListAsync(),
    Dersler = await _context.Dersler.ToListAsync()
          };

        return View(model);
        }

        #region Fakülte Ýþlemleri
[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FakulteEkle(string fakulteAdi)
  {
       if (string.IsNullOrWhiteSpace(fakulteAdi))
            {
      TempData["Error"] = "Fakülte adý boþ olamaz.";
     return RedirectToAction(nameof(Index));
            }

     var mevcutFakulte = await _context.Fakulteler.AnyAsync(f => f.Ad == fakulteAdi);
   if (mevcutFakulte)
            {
     TempData["Error"] = "Bu fakülte zaten mevcut.";
           return RedirectToAction(nameof(Index));
     }

      _context.Fakulteler.Add(new Fakulte { Ad = fakulteAdi });
            await _context.SaveChangesAsync();

 TempData["Success"] = "Fakülte baþarýyla eklendi.";
    return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
   public async Task<IActionResult> FakulteSil(int id)
        {
 var fakulte = await _context.Fakulteler.FindAsync(id);
  if (fakulte == null)
    {
        TempData["Error"] = "Fakülte bulunamadý.";
       return RedirectToAction(nameof(Index));
     }

            var hocaSayisi = await _context.Hocalar.CountAsync(h => h.FakulteId == id);

            if (hocaSayisi > 0)
         {
        TempData["Error"] = $"Bu fakülteye baðlý {hocaSayisi} hoca var. Önce hocalarý silin.";
     return RedirectToAction(nameof(Index));
            }

            _context.Fakulteler.Remove(fakulte);
     await _context.SaveChangesAsync();

        TempData["Success"] = "Fakülte silindi.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Hoca Ýþlemleri
[HttpPost]
        [ValidateAntiForgeryToken]
  public async Task<IActionResult> HocaEkle(int fakulteId, int[] dersIds, string rutbe, string adSoyad)
        {
            if (fakulteId == 0 || string.IsNullOrWhiteSpace(adSoyad))
          {
                TempData["Error"] = "Fakülte ve ad soyad zorunludur.";
        return RedirectToAction(nameof(Index));
      }

            if (dersIds == null || !dersIds.Any())
        {
           TempData["Error"] = "En az bir ders seçmelisiniz.";
         return RedirectToAction(nameof(Index));
            }

            var hoca = new Hoca
   {
                FakulteId = fakulteId,
     Rutbe = rutbe ?? "",
AdSoyad = adSoyad,
         AktifMi = true
      };

      _context.Hocalar.Add(hoca);
 await _context.SaveChangesAsync();

     // Hoca-Ders iliþkilerini ekle
      foreach (var dersId in dersIds)
 {
        var hocaDers = new HocaDers
         {
        HocaId = hoca.Id,
         DersId = dersId
           };
     _context.HocaDersler.Add(hocaDers);
       }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Hoca baþarýyla eklendi. ({dersIds.Length} ders ile iliþkilendirildi)";
    return RedirectToAction(nameof(Index));
        }

        // Hoca Düzenleme Sayfasý - GET
     [HttpGet]
        public async Task<IActionResult> HocaEdit(int id)
        {
            var hoca = await _context.Hocalar
                .Include(h => h.Fakulte)
  .Include(h => h.Dersler)
                .Include(h => h.HocaDersler)
                .ThenInclude(hd => hd.Ders)
.FirstOrDefaultAsync(h => h.Id == id);

            if (hoca == null)
        {
            TempData["Error"] = "Hoca bulunamadý.";
        return RedirectToAction(nameof(Index));
          }

    return View(hoca);
    }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HocaSil(int id)
        {
            var hoca = await _context.Hocalar.FindAsync(id);
         if (hoca == null)
 {
TempData["Error"] = "Hoca bulunamadý.";
         return RedirectToAction(nameof(Index));
     }

  var dersSayisi = await _context.DersProgrami.CountAsync(d => d.HocaId == id);
      if (dersSayisi > 0)
            {
     TempData["Error"] = $"Bu hocanýn {dersSayisi} dersi var. Önce dersleri silin.";
       return RedirectToAction(nameof(Index));
            }

   _context.Hocalar.Remove(hoca);
         await _context.SaveChangesAsync();

    TempData["Success"] = "Hoca silindi.";
            return RedirectToAction(nameof(Index));
        }

   [HttpPost]
 [ValidateAntiForgeryToken]
   public async Task<IActionResult> HocaDurumDegistir(int id)
        {
    var hoca = await _context.Hocalar.FindAsync(id);
if (hoca == null)
        {
    TempData["Error"] = "Hoca bulunamadý.";
return RedirectToAction(nameof(Index));
         }

         hoca.AktifMi = !hoca.AktifMi;
        await _context.SaveChangesAsync();

            TempData["Success"] = $"Hoca {(hoca.AktifMi ? "aktif" : "pasif")} yapýldý.";
 return RedirectToAction(nameof(Index));
      }

        // Hocadan ders sil
    [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HocadanDersSil(int hocaId, int dersId)
        {
       var ders = await _context.DersProgrami.FindAsync(dersId);
         if (ders == null)
            {
    TempData["Error"] = "Ders bulunamadý.";
       return RedirectToAction(nameof(HocaEdit), new { id = hocaId });
        }

       _context.DersProgrami.Remove(ders);
    await _context.SaveChangesAsync();

       TempData["Success"] = "Ders baþarýyla silindi.";
     return RedirectToAction(nameof(HocaEdit), new { id = hocaId });
        }
        #endregion

        #region Ders Ýþlemleri
   [HttpPost]
        [ValidateAntiForgeryToken]
 public async Task<IActionResult> DersEkle(string dersAdi)
        {
 if (string.IsNullOrWhiteSpace(dersAdi))
            {
         TempData["Error"] = "Ders adý zorunludur.";
       return RedirectToAction(nameof(Index));
   }

            var mevcutDers = await _context.Dersler.AnyAsync(d => d.DersAdi == dersAdi);
 if (mevcutDers)
  {
   TempData["Error"] = "Bu ders zaten mevcut.";
    return RedirectToAction(nameof(Index));
  }

 var ders = new Ders
       {
                DersAdi = dersAdi
        };

            _context.Dersler.Add(ders);
            await _context.SaveChangesAsync();

    TempData["Success"] = "Ders baþarýyla eklendi.";
 return RedirectToAction(nameof(Index));
     }

     [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DersSil(int id)
        {
    var ders = await _context.Dersler.FindAsync(id);
            if (ders == null)
    {
            TempData["Error"] = "Ders bulunamadý.";
           return RedirectToAction(nameof(Index));
 }

            // Baðlý ders programlarý var mý kontrol et
    var programSayisi = await _context.DersProgrami.CountAsync(d => d.DersId == id);
          if (programSayisi > 0)
 {
           TempData["Error"] = $"Bu ders {programSayisi} ders programýnda kullanýlýyor. Önce ders programlarýný silin.";
    return RedirectToAction(nameof(Index));
       }

         _context.Dersler.Remove(ders);
     await _context.SaveChangesAsync();

    TempData["Success"] = "Ders silindi.";
            return RedirectToAction(nameof(Index));
        }

        // Ders Detaylarý
  [HttpGet]
     public async Task<IActionResult> DersDetails(int id)
        {
        var ders = await _context.Dersler.FirstOrDefaultAsync(d => d.Id == id);
  if (ders == null)
            {
             TempData["Error"] = "Ders bulunamadý.";
     return RedirectToAction(nameof(Index));
        }

            // Bu dersi veren hocalar
        var dersHocalari = await _context.HocaDersler
             .Include(hd => hd.Hoca)
     .ThenInclude(h => h.Fakulte)
      .Where(hd => hd.DersId == id)
  .Select(hd => hd.Hoca)
           .ToListAsync();

  // Bu dersin yer aldýðý ders programlarý
            var dersProgramlari = await _context.DersProgrami
         .Include(dp => dp.Hoca)
    .Include(dp => dp.Fakulte)
           .Where(dp => dp.DersId == id)
  .OrderBy(dp => dp.DersGunu)
     .ThenBy(dp => dp.DersSaati)
 .ToListAsync();

            var viewModel = new DersDetayAyarlarViewModel
  {
                Ders = ders,
        DersHocalari = dersHocalari,
        DersProgramlari = dersProgramlari
    };

      return View(viewModel);
      }
        #endregion
  }

    // ViewModel
    public class AyarlarViewModel
    {
        public List<Fakulte> Fakulteler { get; set; } = new();
        public List<Hoca> Hocalar { get; set; } = new();
        public List<Ders> Dersler { get; set; } = new();
    }
}
