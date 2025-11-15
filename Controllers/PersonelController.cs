using DocumentFormat.OpenXml.Drawing.Charts;
using Etutlist.Models;
using Etutlist.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class PersonelController : Controller
    {
  private readonly AppDbContext _context;
        public PersonelController(AppDbContext context) => _context = context;

        // ? SOFT DELETE: ViewModel kullan
        public async Task<IActionResult> Index()
   {
      var vm = new PersonelIndexViewModel
    {
    AktifPersoneller = await _context.Personeller
            .Where(p => p.AktifMi)
        .ToListAsync(),
      PasifPersoneller = await _context.Personeller
         .Where(p => !p.AktifMi)
       .ToListAsync()
            };
            
            return View(vm);
        }

      public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Personel personel)
  {
     if (ModelState.IsValid)
            {
    var people = await _context.Personeller
    .Where(p => p.AktifMi)
          .ToListAsync();
       
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

   personel.AktifMi = true;

         _context.Add(personel);
        await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
 }
      return View(personel);
      }

        public async Task<IActionResult> Edit(int id)
        {
            var personel = await _context.Personeller.FindAsync(id);
if (personel == null) return NotFound();

     var start = DateTime.Today.AddMonths(-3);
            var end = DateTime.Today.AddMonths(3);

            var etutler = await _context.Etutler
    .Where(e => e.PersonelId == id && e.Tarih.Date >= start.Date && e.Tarih.Date <= end.Date)
        .OrderBy(e => e.Tarih)
        .ToListAsync();

       ViewBag.Etutler = etutler;
            return View(personel);
        }

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
     var dbEntity = await _context.Personeller.FindAsync(id);
 if (dbEntity == null) return NotFound();

    dbEntity.Ad = personel.Ad;
       dbEntity.Rutbe = personel.Rutbe;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
 public async Task<IActionResult> RemoveEtutFromPersonel(int etutId, int personelId)
      {
 using var tx = await _context.Database.BeginTransactionAsync();
            try
          {
                var etut = await _context.Etutler.Include(e => e.Personel).FirstOrDefaultAsync(e => e.Id == etutId);
   if (etut == null) return NotFound();

       if (etut.PersonelId != personelId)
            {
     TempData["Error"] = "Etüt bu personele ait deðil.";
           return RedirectToAction("Edit", new { id = personelId });
            }

        if (etut.Tarih.DayOfWeek == DayOfWeek.Sunday)
        etut.Personel.PazarSayisi = Math.Max(0, etut.Personel.PazarSayisi - 1);
           else
    {
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

        private bool PersonelExists(int id) =>
         _context.Personeller.Any(e => e.Id == id);

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
            if (personel == null)
     {
    TempData["Error"] = "Personel bulunamadý.";
   return RedirectToAction(nameof(Index));
          }

    personel.AktifMi = false;
          _context.Personeller.Update(personel);
          await _context.SaveChangesAsync();

        TempData["Success"] = $"{personel.Ad} pasif hale getirildi. Eski kayýtlar korundu.";
   return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
  {
          var personel = await _context.Personeller.FindAsync(id);
            if (personel == null)
          {
     TempData["Error"] = "Personel bulunamadý.";
      return RedirectToAction(nameof(Index));
        }

            personel.AktifMi = true;
  _context.Personeller.Update(personel);
     await _context.SaveChangesAsync();

            TempData["Success"] = $"{personel.Ad} tekrar aktif hale getirildi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
