using DocumentFormat.OpenXml.Drawing.Charts;
using Etutlist.Models;
using Etutlist.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Drawing;

namespace Etutlist.Controllers
{
    public class PersonelController : Controller
    {
        private readonly AppDbContext _context;
        public PersonelController(AppDbContext context) => _context = context;

        // ?? SOFT DELETE: ViewModel kullan + Ortalama deðerleri hesapla
        public async Task<IActionResult> Index()
        {
            var aktifPersoneller = await _context.Personeller
                .Where(p => p.AktifMi)
                .ToListAsync();

            var pasifPersoneller = await _context.Personeller
                .Where(p => !p.AktifMi)
                .ToListAsync();

            var vm = new PersonelIndexViewModel
            {
                AktifPersoneller = aktifPersoneller,
                PasifPersoneller = pasifPersoneller
            };

            // ? Ortalama deðerleri hesapla (aktif personeller için)
            if (aktifPersoneller.Any())
            {
                ViewBag.OrtalamaHaftaIci = aktifPersoneller.Average(p => (double)p.HaftaIciSayisi);
                ViewBag.OrtalamaPazar = aktifPersoneller.Average(p => (double)p.PazarSayisi);
                ViewBag.OrtalamaOzelGun = aktifPersoneller.Average(p => (double)p.OzelGunSayisi);
                ViewBag.OrtalamaYedek = aktifPersoneller.Average(p => (double)p.YedekSayisi);
                ViewBag.OrtalamaToplam = aktifPersoneller.Average(p => 
                    (double)(p.HaftaIciSayisi + p.PazarSayisi + p.OzelGunSayisi));
            }
            else
            {
                ViewBag.OrtalamaHaftaIci = 0.0;
                ViewBag.OrtalamaPazar = 0.0;
                ViewBag.OrtalamaOzelGun = 0.0;
                ViewBag.OrtalamaYedek = 0.0;
                ViewBag.OrtalamaToplam = 0.0;
            }
            
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

        public async Task<IActionResult> ExportToExcel()
        {
            var personeller = await _context.Personeller
                .Where(p => p.AktifMi)
                .OrderBy(p => p.Rutbe)
                .ThenBy(p => p.Ad)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Personel Listesi");

            // Baþlýk satýrý
            worksheet.Cell(1, 1).Value = "S. NO";
            worksheet.Cell(1, 2).Value = "RÜTBE";
            worksheet.Cell(1, 3).Value = "AD SOYAD";
            worksheet.Cell(1, 4).Value = "HAFTA ÝÇÝ";
            worksheet.Cell(1, 5).Value = "PAZAR";
            worksheet.Cell(1, 6).Value = "ÖZEL GÜN";
            worksheet.Cell(1, 7).Value = "YEDEK";
            worksheet.Cell(1, 8).Value = "TOPLAM";

            // Baþlýk stili
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 12;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Veri satýrlarý
            int row = 2;
            int siraNo = 1;
            foreach (var personel in personeller)
            {
                int toplam = personel.HaftaIciSayisi + personel.PazarSayisi + personel.OzelGunSayisi;

                worksheet.Cell(row, 1).Value = siraNo;
                worksheet.Cell(row, 2).Value = personel.Rutbe;
                worksheet.Cell(row, 3).Value = personel.Ad;
                worksheet.Cell(row, 4).Value = personel.HaftaIciSayisi;
                worksheet.Cell(row, 5).Value = personel.PazarSayisi;
                worksheet.Cell(row, 6).Value = personel.OzelGunSayisi;
                worksheet.Cell(row, 7).Value = personel.YedekSayisi;
                worksheet.Cell(row, 8).Value = toplam;

                // Sayýsal hücreleri ortala
                for (int col = 1; col <= 8; col++)
                {
                    worksheet.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Toplam hücresini vurgula
                worksheet.Cell(row, 8).Style.Font.Bold = true;
                worksheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.LightYellow;

                row++;
                siraNo++;
            }

            // Ortalama satýrý
            if (personeller.Any())
            {
                worksheet.Cell(row, 1).Value = "ORTALAMA";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                worksheet.Range(row, 1, row, 3).Merge();
                worksheet.Range(row, 1, row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(row, 4).Value = Math.Round(personeller.Average(p => (double)p.HaftaIciSayisi), 2);
                worksheet.Cell(row, 5).Value = Math.Round(personeller.Average(p => (double)p.PazarSayisi), 2);
                worksheet.Cell(row, 6).Value = Math.Round(personeller.Average(p => (double)p.OzelGunSayisi), 2);
                worksheet.Cell(row, 7).Value = Math.Round(personeller.Average(p => (double)p.YedekSayisi), 2);
                worksheet.Cell(row, 8).Value = Math.Round(personeller.Average(p => 
                    (double)(p.HaftaIciSayisi + p.PazarSayisi + p.OzelGunSayisi)), 2);

                var avgRange = worksheet.Range(row, 1, row, 8);
                avgRange.Style.Font.Bold = true;
                avgRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                avgRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                avgRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            }

            // Kolon geniþlikleri
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 30;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 10;
            worksheet.Column(6).Width = 12;
            worksheet.Column(7).Width = 10;
            worksheet.Column(8).Width = 12;

            // Excel dosyasýný oluþtur
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Personel_Listesi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
    }
}
