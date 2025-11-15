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
        public async Task<IActionResult> HocaEkle(int FakulteId, int[] dersIds, string rutbe, string adSoyad)
        {
            if (FakulteId == 0 || string.IsNullOrWhiteSpace(adSoyad))
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
                FakulteId = FakulteId,
                Rutbe = rutbe ?? "",
                AdSoyad = adSoyad,
                AktifMi = true
            };

            _context.Hocalar.Add(hoca);
            await _context.SaveChangesAsync();

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

        [HttpGet]
        public async Task<IActionResult> DersDetails(int id)
        {
            var ders = await _context.Dersler.FirstOrDefaultAsync(d => d.Id == id);
            if (ders == null)
            {
                TempData["Error"] = "Ders bulunamadý.";
                return RedirectToAction(nameof(Index));
            }

            var dersHocalari = await _context.HocaDersler
                .Include(hd => hd.Hoca)
                    .ThenInclude(h => h.Fakulte)
                .Where(hd => hd.DersId == id)
                .Select(hd => hd.Hoca)
                .ToListAsync();

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

        #region Telafi Ayarlarý

        [HttpGet]
        public async Task<IActionResult> TelafiAyarlari()
        {
            // Navigation property'leri yükle
            var ayarlar = await _context.TaburTelafiAyarlari
                .Include(t => t.Tabur)
                    .ThenInclude(tab => tab.Fakulte)
                .ToListAsync();

            // Taburlarý yükle
            ViewBag.Taburlar = await _context.Taburlar
                .Include(t => t.Fakulte)
                .ToListAsync();
            
            ViewBag.Gunler = new List<string> { "Pazartesi", "Salý", "Çarþamba", "Perþembe", "Cuma" };
            ViewBag.Saatler = Enumerable.Range(1, 9).ToList();

            return View(ayarlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TelafiAyariEkle(TaburTelafiAyarlari ayar)
        {
            if (ayar.TaburId == 0)
            {
                TempData["Error"] = "Tabur seçmelisiniz.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            // Ayný tabur için ayný günde ayar var mý kontrol et
            var mevcutAyar = await _context.TaburTelafiAyarlari
                .AnyAsync(f => f.TaburId == ayar.TaburId && f.TelafiYapilamayacakGun == ayar.TelafiYapilamayacakGun);

            if (mevcutAyar)
            {
                TempData["Error"] = "Bu tabur için bu gün için ayar zaten mevcut.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            // TelafiMaxBitisSaati'yi set et (default 9)
            ayar.TelafiMaxBitisSaati = 9;

            _context.TaburTelafiAyarlari.Add(ayar);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Telafi ayarý baþarýyla eklendi.";
            return RedirectToAction(nameof(TelafiAyarlari));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TelafiAyariDuzenle(int id, TaburTelafiAyarlari ayar)
        {
            if (id != ayar.Id)
            {
                TempData["Error"] = "Geçersiz iþlem.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            var mevcutAyar = await _context.TaburTelafiAyarlari.FindAsync(id);
            if (mevcutAyar == null)
            {
                TempData["Error"] = "Ayar bulunamadý.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            mevcutAyar.TelafiYapilamayacakGun = ayar.TelafiYapilamayacakGun;
            mevcutAyar.TelafiBaslamaSaati = ayar.TelafiBaslamaSaati;
            mevcutAyar.TelafiYapilamayacakDersSaatleri = ayar.TelafiYapilamayacakDersSaatleri;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Telafi ayarý baþarýyla güncellendi.";
            return RedirectToAction(nameof(TelafiAyarlari));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TelafiAyariSil(int id)
        {
            var ayar = await _context.TaburTelafiAyarlari.FindAsync(id);
            if (ayar == null)
            {
                TempData["Error"] = "Ayar bulunamadý.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            _context.TaburTelafiAyarlari.Remove(ayar);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Telafi ayarý silindi.";
            return RedirectToAction(nameof(TelafiAyarlari));
        }

        #endregion

        #region Tehlikeli Ýþlemler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TumunuSil(string onay)
        {
            if (onay != "TUMU")
            {
                TempData["Error"] = "Onay kodu yanlýþ! Lütfen 'TUMU' yazýn.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var telafiDersler = await _context.TelafiDersler.ToListAsync();
                _context.TelafiDersler.RemoveRange(telafiDersler);

                var dersProgramlari = await _context.DersProgrami.ToListAsync();
                _context.DersProgrami.RemoveRange(dersProgramlari);

                var hocaDersler = await _context.HocaDersler.ToListAsync();
                _context.HocaDersler.RemoveRange(hocaDersler);

                var hocalar = await _context.Hocalar.ToListAsync();
                _context.Hocalar.RemoveRange(hocalar);

                var dersler = await _context.Dersler.ToListAsync();
                _context.Dersler.RemoveRange(dersler);

                await _context.SaveChangesAsync();

                TempData["Success"] = $"TÜMÜ SÝLÝNDÝ! {telafiDersler.Count} telafi dersi, {dersProgramlari.Count} ders programý, {hocalar.Count} hoca ve {dersler.Count} ders silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }

    public class AyarlarViewModel
    {
        public List<Fakulte> Fakulteler { get; set; } = new();
        public List<Hoca> Hocalar { get; set; } = new();
        public List<Ders> Dersler { get; set; } = new();
    }
}
