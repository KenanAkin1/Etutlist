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
            var model = new Etutlist.ViewModels.AyarlarViewModel
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

        #region Fakülte İşlemleri
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FakulteEkle(string fakulteAdi)
        {
            if (string.IsNullOrWhiteSpace(fakulteAdi))
            {
                TempData["Error"] = "Fakülte adı boş olamaz.";
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

            TempData["Success"] = "Fakülte başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FakulteSil(int id)
        {
            var fakulte = await _context.Fakulteler.FindAsync(id);
            if (fakulte == null)
            {
                TempData["Error"] = "Fakülte bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var hocaSayisi = await _context.Hocalar.CountAsync(h => h.FakulteId == id);

            if (hocaSayisi > 0)
            {
                TempData["Error"] = $"Bu fakülteye bağlı {hocaSayisi} hoca var. Önce hocaları silin.";
                return RedirectToAction(nameof(Index));
            }

            _context.Fakulteler.Remove(fakulte);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Fakülte silindi.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Hoca İşlemleri
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

            TempData["Success"] = $"Hoca başarıyla eklendi. ({dersIds.Length} ders ile ilişkilendirildi)";
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
                TempData["Error"] = "Hoca bulunamadı.";
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
                TempData["Error"] = "Hoca bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var dersSayisi = await _context.DersProgrami.CountAsync(d => d.HocaId == id);
            if (dersSayisi > 0)
            {
                TempData["Error"] = $"Bu hocanın {dersSayisi} dersi var. Önce dersleri silin.";
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
                TempData["Error"] = "Hoca bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            hoca.AktifMi = !hoca.AktifMi;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Hoca {(hoca.AktifMi ? "aktif" : "pasif")} yapıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HocadanDersSil(int hocaId, int dersId)
        {
            var ders = await _context.DersProgrami.FindAsync(dersId);
            if (ders == null)
            {
                TempData["Error"] = "Ders bulunamadı.";
                return RedirectToAction(nameof(HocaEdit), new { id = hocaId });
            }

            _context.DersProgrami.Remove(ders);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ders başarıyla silindi.";
            return RedirectToAction(nameof(HocaEdit), new { id = hocaId });
        }
        #endregion

        #region Ders İşlemleri
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DersEkle(string dersAdi)
        {
            if (string.IsNullOrWhiteSpace(dersAdi))
            {
                TempData["Error"] = "Ders adı zorunludur.";
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

            TempData["Success"] = "Ders başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DersSil(int id)
        {
            var ders = await _context.Dersler.FindAsync(id);
            if (ders == null)
            {
                TempData["Error"] = "Ders bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var programSayisi = await _context.DersProgrami.CountAsync(d => d.DersId == id);
            if (programSayisi > 0)
            {
                TempData["Error"] = $"Bu ders {programSayisi} ders programında kullanılıyor. Önce ders programlarını silin.";
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
                TempData["Error"] = "Ders bulunamadı.";
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

        #region Telafi Ayarları

        [HttpGet]
        public async Task<IActionResult> TelafiAyarlari()
        {
            var ayarlar = await _context.TaburTelafiAyarlari
                .Include(t => t.Tabur)
                    .ThenInclude(tab => tab.Fakulte)
                .ToListAsync();

            ViewBag.Taburlar = await _context.Taburlar
                .Include(t => t.Fakulte)
                .ToListAsync();
            
            ViewBag.Fakulteler = await _context.Fakulteler.ToListAsync();
            ViewBag.Gunler = new List<string> { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };
            ViewBag.Saatler = Enumerable.Range(1, 9).ToList();

            return View(ayarlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaburEkle(int FakulteId, string TaburAdi, int MinKisimNo, int MaxKisimNo)
        {
            if (FakulteId == 0 || string.IsNullOrWhiteSpace(TaburAdi))
            {
                TempData["Error"] = "Fakülte ve tabur adı zorunludur.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            if (MinKisimNo <= 0 || MaxKisimNo <= 0)
            {
                TempData["Error"] = "Kısım numaraları 0'dan büyük olmalıdır.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            if (MinKisimNo > MaxKisimNo)
            {
                TempData["Error"] = "Min Kısım No, Max Kısım No'dan büyük olamaz.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            var tabur = new Tabur
            {
                FakulteId = FakulteId,
                TaburAdi = TaburAdi,
                MinKisimNo = MinKisimNo,
                MaxKisimNo = MaxKisimNo
            };

            _context.Taburlar.Add(tabur);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tabur başarıyla eklendi: {TaburAdi} (Kısım {MinKisimNo}-{MaxKisimNo})";
            return RedirectToAction(nameof(TelafiAyarlari));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaburSil(int id)
        {
            var tabur = await _context.Taburlar.FindAsync(id);
            if (tabur == null)
            {
                TempData["Error"] = "Tabur bulunamadı.";
                return RedirectToAction(nameof(TelafiAyarlari));
            }

            _context.Taburlar.Remove(tabur);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tabur silindi.";
            return RedirectToAction(nameof(TelafiAyarlari));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TelafiAyarlariKaydet(int TaburId, List<GunAyari> Gunler)
        {
            try
            {
                var mevcutAyarlar = await _context.TaburTelafiAyarlari
                    .Where(t => t.TaburId == TaburId)
                    .ToListAsync();
                _context.TaburTelafiAyarlari.RemoveRange(mevcutAyarlar);

                foreach (var gun in Gunler)
                {
                    if (!gun.TelafiYapilmaz && gun.BaslamaSaati > 0)
                    {
                        var ayar = new TaburTelafiAyarlari
                        {
                            TaburId = TaburId,
                            TelafiYapilacakGun = gun.Gun,
                            TelafiBaslamaSaati = gun.BaslamaSaati,
                            TelafiMaxBitisSaati = 9,
                            TelafiYapilamayacakDersSaatleri = gun.AtlanacakSaatler
                        };
                        _context.TaburTelafiAyarlari.Add(ayar);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Telafi ayarları başarıyla kaydedildi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }

            return RedirectToAction(nameof(TelafiAyarlari));
        }

        #endregion

        #region Tehlikeli İşlemler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TumunuSil(string onay)
        {
            if (onay != "TUMU")
            {
                TempData["Error"] = "Onay kodu yanlış! Lütfen 'TUMU' yazın.";
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

                TempData["Success"] = $"TÜMÜ SİLİNDİ! {telafiDersler.Count} telafi dersi, {dersProgramlari.Count} ders programı, {hocalar.Count} hoca ve {dersler.Count} ders silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
