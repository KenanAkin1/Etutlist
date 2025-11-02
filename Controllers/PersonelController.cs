using DocumentFormat.OpenXml.Drawing.Charts;
using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class PersonelController : Controller
    {
        private readonly AppDbContext _context;
        public PersonelController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            View(await _context.Personeller.ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Personel personel)
        {
            if (ModelState.IsValid)
            {
                var people = await _context.Personeller.ToListAsync();
                if (people.Any())
                {
                    personel.HaftaIciSayisi = (int)Math.Round(people.Average(p => p.HaftaIciSayisi));
                    personel.PazarSayisi = (int)Math.Round(people.Average(p => p.PazarSayisi));
                    personel.OzelGunSayisi = (int)Math.Round(people.Average(p => p.OzelGunSayisi));
                    personel.YedekSayisi = (int)Math.Round(people.Average(p => p.YedekSayisi));
                }
                else
                {
                    personel.HaftaIciSayisi = 0;
                    personel.PazarSayisi = 0;
                    personel.OzelGunSayisi = 0;
                    personel.YedekSayisi = 0;
                }

                _context.Add(personel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(personel);
        }

        // EDIT GET: yüklenen personel ve atandığı etütler
        public async Task<IActionResult> Edit(int id)
        {
            var personel = await _context.Personeller.FindAsync(id);
            if (personel == null) return NotFound();

            // Yakın tarih aralığı: geçmiş 3 ay ile gelecek 3 ay (ayarlanabilir)
            var start = DateTime.Today.AddMonths(-3);
            var end = DateTime.Today.AddMonths(3);

            var etutler = await _context.Etutler
                .Where(e => e.PersonelId == id && e.Tarih.Date >= start.Date && e.Tarih.Date <= end.Date)
                .OrderBy(e => e.Tarih)
                .ToListAsync();

            ViewBag.Etutler = etutler;
            return View(personel);
        }

        // EDIT POST: personel güncelle (bilgi + sayaçlar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Personel personel)
        {
            if (id != personel.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var start = DateTime.Today.AddMonths(-3);
                var end = DateTime.Today.AddMonths(3);
                ViewBag.Etutler = await _context.Etutler
                    .Where(e => e.PersonelId == id && e.Tarih.Date >= start.Date && e.Tarih.Date <= end.Date)
                    .OrderBy(e => e.Tarih)
                    .ToListAsync();
                return View(personel);
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Yalnızca istenen alanları güncellemek istersen burada selective update yapılabilir.
                var dbEntity = await _context.Personeller.FindAsync(id);
                if (dbEntity == null) return NotFound();

                dbEntity.Ad = personel.Ad;
                dbEntity.Rutbe = personel.Rutbe;
                // Sayaçlar manuel düzenlenebiliyorsa alttaki değerleri ata
                dbEntity.HaftaIciSayisi = Math.Max(0, personel.HaftaIciSayisi);
                dbEntity.PazarSayisi = Math.Max(0, personel.PazarSayisi);
                dbEntity.OzelGunSayisi = Math.Max(0, personel.OzelGunSayisi);
                dbEntity.YedekSayisi = Math.Max(0, personel.YedekSayisi);

                _context.Personeller.Update(dbEntity);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                TempData["Success"] = "Personel güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                if (!PersonelExists(id)) return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = ex.Message;
                var start = DateTime.Today.AddMonths(-3);
                var end = DateTime.Today.AddMonths(3);
                ViewBag.Etutler = await _context.Etutler
                    .Where(e => e.PersonelId == id && e.Tarih.Date >= start.Date && e.Tarih.Date <= end.Date)
                    .OrderBy(e => e.Tarih)
                    .ToListAsync();
                return View(personel);
            }
        }

        // POST: Personel/RemoveEtutFromPersonel - etütü sil ve sayaçları geri al
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEtutFromPersonel(int etutId, int personelId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var etut = await _context.Etutler.Include(e => e.Personel).FirstOrDefaultAsync(e => e.Id == etutId);
                if (etut == null) return NotFound();

                // Doğrula: etüt gerçekten ilgili personel tarafından mı tutuluyor
                if (etut.PersonelId != personelId)
                {
                    TempData["Error"] = "Etüt bu personele ait değil.";
                    return RedirectToAction("Edit", new { id = personelId });
                }

                // O aya ait özel gün setine ihtiyaç yok; gün tipine göre sayaç azalt
                if (etut.Tarih.DayOfWeek == DayOfWeek.Sunday)
                    etut.Personel.PazarSayisi = Math.Max(0, etut.Personel.PazarSayisi - 1);
                else
                {
                    // özel gün kontrolü: eğer OzelGunler tablonuz varsa DB'den kontrol et
                    bool isOzel = await _context.OzelGunler.AnyAsync(o => o.Tarih.Date == etut.Tarih.Date);
                    if (isOzel)
                        etut.Personel.OzelGunSayisi = Math.Max(0, etut.Personel.OzelGunSayisi - 1);
                    else
                        etut.Personel.HaftaIciSayisi = Math.Max(0, etut.Personel.HaftaIciSayisi - 1);
                }

                _context.Etutler.Remove(etut);
                _context.Personeller.Update(etut.Personel);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Etüt silindi ve sayaçlar güncellendi.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Edit", new { id = personelId });
        }

        // DELETE helpers
        private bool PersonelExists(int id) =>
            _context.Personeller.Any(e => e.Id == id);

        // GET: Personel/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var personel = await _context.Personeller.FindAsync(id.Value);
            if (personel == null) return NotFound();

            return View(personel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personel = await _context.Personeller.FindAsync(id);
            if (personel == null) return RedirectToAction(nameof(Index));

            bool hasEtut = await _context.Etutler.AnyAsync(e => e.PersonelId == id);
            if (hasEtut)
            {
                TempData["DeleteError"] = "Bu personel silinemiyor. Etüt kayıtları mevcut.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var mazeretler = _context.Mazeretler.Where(m => m.PersonelId == id);
            _context.Mazeretler.RemoveRange(mazeretler);

            _context.Personeller.Remove(personel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}