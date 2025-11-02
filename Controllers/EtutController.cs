using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClosedXML.Excel;
using Etutlist.Models;
using Etutlist.Services;
using Microsoft.AspNetCore.Mvc;

namespace Etutlist.Controllers
{
    public class EtutController : Controller
    {
        private readonly EtutPlanlamaService _svc;
        public EtutController(EtutPlanlamaService svc) => _svc = svc;

        // Helper: year/month normalize
        private static (int year, int month) NormalizeYearMonth(int year, int month)
        {
            // month can be outside 1..12; normalize to valid year/month
            year += (month - 1) / 12;
            month = ((month - 1) % 12) + 1;
            if (month <= 0) { month += 12; year -= 1; } // defensive
            return (year, month);
        }

        // GET: /Etut -> MonthlyPlan'a yönlendirir (her zaman bugünün yılı/ayı)
        public IActionResult Index()
        {
            var now = DateTime.Now;
            return RedirectToAction(nameof(MonthlyPlan), new { yil = now.Year, ay = now.Month });
        }

        // GET: Plan sayfası (yil/ay opsiyonel)
        public async Task<IActionResult> MonthlyPlan(int? yil, int? ay)
        {
            var now = DateTime.Now;
            int rawY = yil ?? now.Year;
            int rawM = ay ?? now.Month;
            (int useYil, int useAy) = NormalizeYearMonth(rawY, rawM);

            var startDate = new DateTime(useYil, useAy, 1);
            var vm = new MonthlyPlanViewModel
            {
                Plan = await _svc.GetPlan(startDate),
                Yedekler = await _svc.PeekMonthlyYedekList(startDate, 15),
                Yil = useYil,
                Ay = useAy
            };

            // Prev/Next hesapla ve ekle (view'da kullan)
            var prev = NormalizeYearMonth(useYil, useAy - 1);
            var next = NormalizeYearMonth(useYil, useAy + 1);
            ViewBag.PrevYil = prev.year; ViewBag.PrevAy = prev.month;
            ViewBag.NextYil = next.year; ViewBag.NextAy = next.month;

            ViewBag.Yil = useYil;
            ViewBag.Ay = useAy;
            return View(vm);
        }

        // POST: Plan oluştur ve yedekleri otomatik üret
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMonthlyPlan(int yil, int ay)
        {
            var (ny, nm) = NormalizeYearMonth(yil, ay);
            var startDate = new DateTime(ny, nm, 1);
            await _svc.GenerateMonthlyPlan(startDate);
            return RedirectToAction(nameof(MonthlyPlan), new { yil = ny, ay = nm });
        }

        // GET: Etut/Manage/5  -> Etüt detay + müsait yedekler + tüm etütler (takas için)
        [HttpGet]
        public async Task<IActionResult> Manage(int id, int? yil, int? ay)
        {
            var etut = await _svc.GetEtutWithPersonelAsync(id);
            if (etut == null) return NotFound();

            var now = DateTime.Now;
            int rawY = yil ?? now.Year;
            int rawM = ay ?? now.Month;
            (int useYil, int useAy) = NormalizeYearMonth(rawY, rawM);
            var startDate = new DateTime(useYil, useAy, 1);

            var musaitYedekler = await _svc.GetAvailableYedeklerForDateAsync(etut.Tarih);
            var tumEtutler = (await _svc.GetPlan(startDate)).SelectMany(kv => kv.Value).ToList();

            var vm = new ManageEtutViewModel
            {
                Etut = etut,
                MusaitYedekler = musaitYedekler,
                TumEtutler = tumEtutler
            };

            ViewBag.Yil = useYil;
            ViewBag.Ay = useAy;
            return View(vm);
        }

        // POST: Etut/ReplaceWithYedek
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplaceWithYedek(int etutId, int yedekPersonelId, int? yil, int? ay)
        {
            try
            {
                await _svc.ReplaceEtutWithYedekAsync(etutId, yedekPersonelId);
                TempData["Success"] = "Yedek başarıyla atanıp etüt güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            var rawY = yil ?? DateTime.Now.Year;
            var rawM = ay ?? DateTime.Now.Month;
            var (ny, nm) = NormalizeYearMonth(rawY, rawM);
            return RedirectToAction(nameof(Manage), new { id = etutId, yil = ny, ay = nm });
        }

        // POST: Etut/SwapPersons
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwapPersons(int etutAId, int etutBId, int? yil, int? ay)
        {
            try
            {
                await _svc.SwapEtutPersonsAsync(etutAId, etutBId);
                TempData["Success"] = "Etütlerdeki kişiler başarıyla takas edildi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            var rawY = yil ?? DateTime.Now.Year;
            var rawM = ay ?? DateTime.Now.Month;
            var (ny, nm) = NormalizeYearMonth(rawY, rawM);
            return RedirectToAction(nameof(MonthlyPlan), new { yil = ny, ay = nm });
        }

        public async Task<IActionResult> ExportMonthlyPlanToExcel(int? yil, int? ay)
        {
            var now = DateTime.Now;
            int rawY = yil ?? now.Year;
            int rawM = ay ?? now.Month;
            var (useYil, useAy) = NormalizeYearMonth(rawY, rawM);
            var startDate = new DateTime(useYil, useAy, 1);

            var plan = await _svc.GetPlan(startDate);
            var yedekler = await _svc.PeekMonthlyYedekList(startDate);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("EtutCizelgesi");

            int r = 1;

            ws.Cell(r, 1).Value = $"{useYil} {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(useAy).ToUpper()} AYI ETÜT ÇİZELGESİ";
            ws.Range(r, 1, r, 6).Merge();
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r += 2;

            ws.Cell(r, 1).Value = "S.NU.";
            ws.Cell(r, 2).Value = "RÜTBE";
            ws.Cell(r, 3).Value = "ADI SOYADI";
            ws.Cell(r, 4).Value = "GÖREVLİ OLDUĞU TARİH";
            ws.Cell(r, 5).Value = "GÖREVLİ OLDUĞU YER";
            ws.Range(r, 1, r, 5).Style.Font.Bold = true;
            ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
            r++;

            string GetGorevYeriForSinif(int? sinifNo) => sinifNo switch
            {
                1 => "ASEM 10'uncu Öğr.Tb. K.lığı",
                2 => "ASEM 11'inci Öğr.Tb. K.lığı",
                3 => "ASEM 12'nci Öğr.Tb. K.lığı",
                4 => "ASEM 13'üncü Öğr.Tb. K.lığı",
                _ => ""
            };

            var orderedDates = plan.Keys.OrderBy(d => d).ToList();
            int seq = 1;
            bool useOrange = true;

            foreach (var date in orderedDates)
            {
                var etutler = plan[date].OrderBy(e => e.SinifNo).ToList();
                if (etutler.Count == 0) continue;

                int startRowForDate = r;
                foreach (var e in etutler)
                {
                    ws.Cell(r, 1).Value = seq++;
                    ws.Cell(r, 2).Value = e.Personel?.Rutbe ?? "";
                    ws.Cell(r, 3).Value = e.Personel?.Ad ?? "";
                    ws.Cell(r, 5).Value = GetGorevYeriForSinif(e.SinifNo);
                    r++;
                }
                int endRowForDate = r - 1;

                ws.Range(startRowForDate, 4, endRowForDate, 4).Merge();
                ws.Cell(startRowForDate, 4).Value = date.ToString("dd.MM.yyyy dddd");
                ws.Cell(startRowForDate, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell(startRowForDate, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var bgColor = useOrange ? XLColor.FromHtml("#FFD8A6") : XLColor.White;
                ws.Range(startRowForDate, 1, endRowForDate, 5).Style.Fill.BackgroundColor = bgColor;

                useOrange = !useOrange;
            }

            r += 2;
            ws.Cell(r, 1).Value = "Etüt Faaliyeti Yedek Personel Görevli İsim Listesi";
            ws.Range(r, 1, r, 5).Merge();
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            r += 2;

            ws.Cell(r, 1).Value = "S.No";
            ws.Cell(r, 2).Value = "Rütbe";
            ws.Cell(r, 3).Value = "Ad Soyad";
            ws.Range(r, 1, r, 3).Style.Font.Bold = true;
            ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
            r++;

            int yseq = 1;
            foreach (var p in yedekler)
            {
                ws.Cell(r, 1).Value = yseq++;
                ws.Cell(r, 2).Value = p.Rutbe ?? "";
                ws.Cell(r, 3).Value = p.Ad ?? "";
                r++;
            }

            for (int c = 1; c <= 5; c++) ws.Column(c).AdjustToContents();
            ws.Rows().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var fileName = $"EtutPlan_{useYil}_{useAy:00}.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // POST: Plan sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMonth(int yil, int ay)
        {
            var (ny, nm) = NormalizeYearMonth(yil, ay);
            await _svc.DeleteMonthlyPlan(new DateTime(ny, nm, 1));
            return RedirectToAction(nameof(MonthlyPlan), new { yil = ny, ay = nm });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateYedekForMonth(int yil, int ay)
        {
            var (ny, nm) = NormalizeYearMonth(yil, ay);
            var startDate = new DateTime(ny, nm, 1);

            // Manuel tek seferde 5 adet üret
            await _svc.GenerateMonthlyYedekList(startDate, 5);

            return RedirectToAction(nameof(MonthlyPlan), new { yil = ny, ay = nm });
        }

    }
}