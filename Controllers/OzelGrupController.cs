using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class OzelGrupController : Controller
    {
        private readonly AppDbContext _context;
        
        public OzelGrupController(AppDbContext context)
        {
            _context = context;
        }

        // GET: OzelGrup
        public async Task<IActionResult> Index()
        {
            var gruplar = await _context.OzelGruplar
                .Include(g => g.Uyeler)
                    .ThenInclude(u => u.Personel)
                .OrderBy(g => g.GrupAdi)
                .ToListAsync();
            
            return View(gruplar);
        }

        // GET: OzelGrup/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AktifPersoneller = await _context.Personeller
                .Where(p => p.AktifMi)
                .OrderBy(p => p.Ad)
                .ToListAsync();
            
            return View();
        }

        // POST: OzelGrup/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string grupAdi, string? aciklama, bool ortalamaKullan, List<int> seciliPersoneller)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(grupAdi))
                {
                    TempData["Error"] = "? Grup adý boþ olamaz!";
                    return RedirectToAction(nameof(Create));
                }

                if (seciliPersoneller == null || seciliPersoneller.Count < 2 || seciliPersoneller.Count > 4)
                {
                    TempData["Error"] = "? Grup 2-4 kiþi arasýnda olmalýdýr!";
                    return RedirectToAction(nameof(Create));
                }

                var grup = new OzelGrup
                {
                    GrupAdi = grupAdi,
                    Aciklama = aciklama,
                    AktifMi = true,
                    OrtalamaKullan = ortalamaKullan
                };

                _context.OzelGruplar.Add(grup);
                await _context.SaveChangesAsync();

                foreach (var personelId in seciliPersoneller)
                {
                    _context.OzelGrupUyeleri.Add(new OzelGrupUyesi
                    {
                        OzelGrupId = grup.Id,
                        PersonelId = personelId
                    });
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"? {grup.GrupAdi} ({seciliPersoneller.Count} kiþi) baþarýyla oluþturuldu!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"? Hata: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: OzelGrup/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var grup = await _context.OzelGruplar
                .Include(g => g.Uyeler)
                    .ThenInclude(u => u.Personel)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grup == null)
            {
                TempData["Error"] = "? Grup bulunamadý!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AktifPersoneller = await _context.Personeller
                .Where(p => p.AktifMi)
                .OrderBy(p => p.Ad)
                .ToListAsync();
            
            return View(grup);
        }

        // POST: OzelGrup/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string grupAdi, string? aciklama, bool aktifMi, bool ortalamaKullan, List<int> seciliPersoneller)
        {
            try
            {
                var grup = await _context.OzelGruplar
                    .Include(g => g.Uyeler)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (grup == null)
                {
                    TempData["Error"] = "? Grup bulunamadý!";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(grupAdi))
                {
                    TempData["Error"] = "? Grup adý boþ olamaz!";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                if (seciliPersoneller == null || seciliPersoneller.Count < 2 || seciliPersoneller.Count > 4)
                {
                    TempData["Error"] = "? Grup 2-4 kiþi arasýnda olmalýdýr!";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                grup.GrupAdi = grupAdi;
                grup.Aciklama = aciklama;
                grup.AktifMi = aktifMi;
                grup.OrtalamaKullan = ortalamaKullan;

                // Eski üyeleri sil
                _context.OzelGrupUyeleri.RemoveRange(grup.Uyeler);

                // Yeni üyeleri ekle
                foreach (var personelId in seciliPersoneller)
                {
                    _context.OzelGrupUyeleri.Add(new OzelGrupUyesi
                    {
                        OzelGrupId = grup.Id,
                        PersonelId = personelId
                    });
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"? {grup.GrupAdi} baþarýyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"? Hata: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // POST: OzelGrup/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var grup = await _context.OzelGruplar
                    .Include(g => g.Uyeler)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (grup != null)
                {
                    var grupAdi = grup.GrupAdi;
                    _context.OzelGruplar.Remove(grup);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"? {grupAdi} silindi!";
                }
                else
                {
                    TempData["Error"] = "? Grup bulunamadý!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"? Silme hatasý: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: OzelGrup/ToggleAktif/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAktif(int id)
        {
            try
            {
                var grup = await _context.OzelGruplar.FindAsync(id);
                if (grup != null)
                {
                    grup.AktifMi = !grup.AktifMi;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"? {grup.GrupAdi} {(grup.AktifMi ? "aktif" : "pasif")} hale getirildi!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"? Hata: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
