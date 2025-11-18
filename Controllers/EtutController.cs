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

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("EtutCizelgesi");

            // Varsayılan stilleri ayarla
            ws.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFFFF"); // Beyaz
            ws.Style.Font.Bold = true;

            int r = 1; // Başlangıç satırı

            // 1. BAŞLIK
            string ayAdi = System.Globalization.CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(useAy).ToUpper();
            ws.Cell(r, 1).Value = $"{ayAdi} AYI ETÜT KONTROL GÖREVLİSİ ÇİZELGESİ";
            ws.Range(r, 1, r, 5).Merge();
            ws.Cell(r, 1).Style.Font.FontSize = 14;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(r, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(r, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#A6C785"); // Açık Yeşil
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#000000"); // Siyah
            r += 1; // Boş satır kaldırıldı

            // 2. SÜTUN BAŞLIKLARI
            int headerRow = r;
            ws.Cell(r, 1).Value = "S.NU.";
            ws.Cell(r, 2).Value = "RÜTBE";
            ws.Cell(r, 3).Value = "ADI SOYADI";
            ws.Cell(r, 4).Value = "GÖREVLİ OLDUĞU TARİH";
            ws.Cell(r, 5).Value = "GÖREVLİ OLDUĞU YER";
            ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#EA9F62"); // Turuncu
            r++;

            // Görev Yeri için helper metod
            string GetGorevYeriForSinif(int? sinifNo) => sinifNo switch
            {
                1 => "ASEM 10'uncu Öğr.Tb. K.lığı",
                2 => "ASEM 11'inci Öğr.Tb. K.lığı",
                3 => "ASEM 12'nci Öğr.Tb. K.lığı",
                4 => "ASEM 13'üncü Öğr.Tb. K.lığı",
                _ => ""
            };

            // 3. VERİ SATIRLARI
            var orderedDates = plan.Keys.OrderBy(d => d).ToList();
            int seq = 1;
            bool useRose = true;
            int dataStartRow = r;

            foreach (var date in orderedDates)
            {
                var etutler = plan[date].OrderBy(e => e.SinifNo).ToList();
                if (etutler.Count == 0) continue;

                int startRowForDate = r;

                ws.Cell(startRowForDate, 4).Value = date.ToString("dd.MM.yyyy dddd", new System.Globalization.CultureInfo("tr-TR"));

                foreach (var e in etutler)
                {
                    ws.Cell(r, 1).Value = seq++;
                    ws.Cell(r, 2).Value = e.Personel?.Rutbe ?? "";
                    ws.Cell(r, 3).Value = e.Personel?.Ad ?? "";
                    ws.Cell(r, 5).Value = GetGorevYeriForSinif(e.SinifNo);
                    r++;
                }
                int endRowForDate = r - 1;

                if (endRowForDate >= startRowForDate)
                {
                    var dateRange = ws.Range(startRowForDate, 4, endRowForDate, 4);
                    dateRange.Merge();
                    dateRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }

                // Gül (Rose) ve Beyaz arka plan (Sütun 1 hariç)
                var bgColor = useRose ? XLColor.FromHtml("#E0A29E") : XLColor.FromHtml("#FFFFFF"); // Gül (Rose) veya Beyaz
                ws.Range(startRowForDate, 2, endRowForDate, 5).Style.Fill.BackgroundColor = bgColor;
                ws.Cell(startRowForDate, 4).Style.Fill.BackgroundColor = bgColor;

                useRose = !useRose;
            }

            int dataEndRow = r - 1;

            // S.NU Sütunu (Veri Kısmı) ORTALI ve TURUNCU
            if (dataEndRow >= dataStartRow)
            {
                var snuColRange = ws.Range(dataStartRow, 1, dataEndRow, 1);
                snuColRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                snuColRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EA9F62"); // Turuncu
            }

            // Kenarlıklar (Başlık satırından verinin sonuna kadar)
            if (dataEndRow >= dataStartRow)
            {
                var dataTableRange = ws.Range(headerRow, 1, dataEndRow, 5);
                dataTableRange.Style.Border.SetTopBorder(XLBorderStyleValues.Thick);
                dataTableRange.Style.Border.SetBottomBorder(XLBorderStyleValues.Thick);
                dataTableRange.Style.Border.SetLeftBorder(XLBorderStyleValues.Thick);
                dataTableRange.Style.Border.SetRightBorder(XLBorderStyleValues.Thick);
                dataTableRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thick);
            }

            // 4. İMZA BLOĞU
            r++;
            ws.Cell(r, 5).Value = "Mehmet MAZLUM";
            ws.Cell(r, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
            ws.Cell(r, 5).Value = "J.Asb.Kd.Bçvş.";
            ws.Cell(r, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
            ws.Cell(r, 5).Value = "Öğt.Şb.Md.V.";
            ws.Cell(r, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;

            // 5. TALİMATLAR
            r += 2;

            // Talimat Başlığı
            int talimatBaslikRow = r;
            ws.Cell(r, 1).Value = "ETÜT KONTROL GÖREVLİSİ TALİMATI";
            ws.Range(r, 1, r, 5).Merge();
            ws.Cell(r, 1).Style.Font.FontSize = 28;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#000000"); // Siyah
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(r, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(r, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FF0000"); // Kırmızı
            ws.Row(r).Height = 73;
            ws.Range(r, 1, r, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
            r++;

            // Talimatları eklemek için yardımcı Action
            Action<int, string> AddInstruction = (int num, string text) => {
                int instructionStartRow = r;
                ws.Cell(r, 1).Value = num;
                ws.Cell(r, 2).Value = text;

                ws.Cell(r, 1).Style.Font.FontSize = 16;
                ws.Cell(r, 2).Style.Font.FontSize = 16;

                ws.Range(r, 2, r, 5).Merge();
                ws.Cell(r, 2).Style.Alignment.WrapText = true;
                ws.Cell(r, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell(r, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var instructionRange = ws.Range(r, 1, r, 5);
                instructionRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                instructionRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thick);

                ws.Row(r).Height = 150;

                r++;
            };

            // Talimat metinleri
            AddInstruction(1, "ETÜT KONTROL GÖREVLİSİ GÖREVİNİ, KANUN, YÖNETMELİK, YÖNERGE İLE EMİR VE TALİMATLARA UYGUN OLARAK YÜRÜTÜR.");
            AddInstruction(2, "GÖREV GÜNLÜK ÇALIŞMA ÇİZELGESİNE GÖRE 1’İNCİ VE 2’NCİ ETÜT SAATLERİNDE(19:30-20:30) İCRA EDİLECEK, GÖREVLİ PERSONEL ETÜT SAATİNDEN EN GEÇ YARIM SAAT ÖNCESİNDE GÖREV MAHALLİNDE BULUNACAKTIR.");
            AddInstruction(3, "ETÜT BAŞLANGICINDA SORMLU OLDUĞU KISIMLARI DOLAŞARAK DERS İLE İLGİLİ ÖĞRENCİLERİN SORULARINI YANITLAR. TB. NÖB. SB.LARI İLE KOORDİNELİ OLARAK ETÜT FAALİYETİNİN MUNTAZAM BİR ŞEKİLDE İCRASINI SAĞLAR, TESPİT ETTİĞİ DİSİPLİNSİZLİKLERİ NÖBETÇİ HEYETİNE BİLDİRİR.");
            AddInstruction(4, "ETÜT ESNASINDA; EMİRLERE AYKIRI DAVRANDIĞI, DİĞER ÖĞRENCİLERİ RAHATSIZ ETTİĞİ VE ETÜT DÜZENİNİ BOZDUĞU TESPİT EDİLEN ÖĞRENCİLER İLE ETÜDE MAZERETSİZ OLARAK KATILMADIKLARI TESPİT EDİLEN ÖĞRENCİLERİ TABUR NÖBETÇİ SUBAYINA BİLDİRİR VE ÖĞRENCİ HAKKINDA GÖZLEM FORMU TANZİM EDEREK İLGİLİ BÖLÜK KOMUTANINA İLETİR.");
            AddInstruction(5, "ÖĞRENCİLERDEN GELEN BİLDİRİMLERE İSTİNADEN AKSAKLIK OLAN DERSLER HAKKINDA İLGİLİ BÖLÜM BAŞKANLARINI BİLGİLENDİRİR.");
            AddInstruction(6, "AY İÇERİSİNDE YAZILAN GÖREV KESİNLİKLE DEĞİŞTİRİLMEYECEK, ZORUNLU NEDENLER (ANİ HASTALIK, KURS, MAZERET İZNİ, İSTİRAHATLİ NÖBET VB.)’SEBEPERDEN DOLAYI GÖREVİ İCRA EDEMEYECEK DURUMDA OLAN PERSONEL ÖNCELİKLİ OLARAK SONRAKİ HAFTALARIN AYNI GÜNÜ İLE GÖREV DEĞİŞİMİNE GİDECEK, VE ETÜT NÖBETİ DEĞİİŞİKLİK FORMUNU DOLDURUP KOLLUK UYG. EĞT.MRK.K.LIĞINA (1. KAT 114 NOLU ODAYA) TESLİM EDECEKTİR.");
            AddInstruction(7, "TEREDDÜTE DÜŞÜLEN KONULARDA GÖREVLİ OLDUKLARI BİRİMLERDEN SORMLU FAKÜLTE DEKANLIĞI/JAMYO MÜDURLÜĞÜ/KOLLUK UYG. EĞT.MRK.K.LIĞINA DANIŞILACAKTIR.");
            AddInstruction(8, "ETÜT GÖREVLENDİRMELERİNİN TALİMATLARA UYGUN ŞEKİLDE İŞLETİLMESİ VE TAKİBİNDEN İLGİLİ FAKÜLTE DEKANLIĞI/JAMYO MÜDURLÜĞÜ/KOLLUK UYG. EĞT.MRK.K.LIĞI SORMLUDUR.");
            AddInstruction(9, "ETÜT GÖREVİ İCRA EDEN TÜM PERSONEL BU TALİMAT ÇERÇEVESİNDE HAREKET EDECEKTİR.");
            AddInstruction(10, "ETÜT GÖREVİ İCRA EDEN TÜM PERSONEL ETÜTÜN YAPILIP YAPILMAYACAĞI İLE İLGİ ÖĞRENCİ KITALAR KOMUTANLIĞI İLE İLETİŞİME GEÇEÇEKTİR.");


            // 6. SÜTUN GENİŞLİKLERİ
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 20;
            ws.Column(3).Width = 25;
            ws.Column(4).Width = 25;
            ws.Column(5).Width = 30;

            // 7. TÜM İÇERİĞE DIŞ KENARLIK (KALIN)
            int lastUsedRow = r - 1;
            var allRange = ws.Range(1, 1, lastUsedRow, 5);
            allRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);


            // 8. DOSYAYI OLUŞTUR VE DÖNDÜR
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
