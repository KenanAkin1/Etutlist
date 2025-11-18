using Etutlist.Data;
using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class OzelGunController : Controller
    {
        private readonly AppDbContext _context;
        public OzelGunController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            View(await _context.OzelGunler.OrderBy(o => o.Tarih).ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OzelGun ozelGun)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(ozelGun);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"✅ {ozelGun.Aciklama} ({ozelGun.Tarih:dd.MM.yyyy}) başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"❌ Hata oluştu: {ex.Message}";
                }
            }
            return View(ozelGun);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var ozelGun = await _context.OzelGunler.FindAsync(id);
            if (ozelGun == null)
            {
                TempData["Error"] = "❌ Özel gün kaydı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            return View(ozelGun);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OzelGun ozelGun)
        {
            if (id != ozelGun.Id)
            {
                TempData["Error"] = "❌ Geçersiz işlem.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ozelGun);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"✅ {ozelGun.Aciklama} ({ozelGun.Tarih:dd.MM.yyyy}) başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await OzelGunExists(ozelGun.Id))
                    {
                        TempData["Error"] = "❌ Özel gün kaydı bulunamadı.";
                        return RedirectToAction(nameof(Index));
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"❌ Hata oluştu: {ex.Message}";
                }
            }
            return View(ozelGun);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var ozelGun = await _context.OzelGunler.FindAsync(id);
            if (ozelGun == null)
            {
                TempData["Error"] = "❌ Özel gün kaydı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            return View(ozelGun);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var ozelGun = await _context.OzelGunler.FindAsync(id);
                if (ozelGun != null)
                {
                    var aciklama = ozelGun.Aciklama;
                    var tarih = ozelGun.Tarih;
                    
                    _context.OzelGunler.Remove(ozelGun);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"✅ {aciklama} ({tarih:dd.MM.yyyy}) başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "❌ Özel gün kaydı bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Silme işlemi başarısız: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> OzelGunExists(int id)
        {
            return await _context.OzelGunler.AnyAsync(e => e.Id == id);
        }
    }
}
