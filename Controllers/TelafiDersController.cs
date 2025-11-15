using Etutlist.Models;
using Etutlist.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Etutlist.Controllers
{
    public class TelafiDersController : Controller
    {
        private readonly TelafiDersService _telafiService;
        private readonly AppDbContext _context;

        public TelafiDersController(TelafiDersService telafiService, AppDbContext context)
        {
            _telafiService = telafiService;
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<IActionResult> Index(int? fakulteId)
        {
            var telafiler = await _telafiService.GetTelafiDerslerAsync(fakulteId);
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
            ViewBag.SelectedFakulteId = fakulteId;
            return View(telafiler);
        }

        [HttpGet]
        public async Task<IActionResult> CreateIkame()
        {
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDersSaatleri(int fakulteId, int hocaId, int kisimNo)
        {
            var dersler = await _context.DersProgrami
                .Where(d => d.FakulteId == fakulteId && d.HocaId == hocaId && d.KisimNo == kisimNo)
                .OrderBy(d => d.DersGunu).ThenBy(d => d.DersSaati)
                .Select(d => new { d.DersGunu, d.DersSaati }).ToListAsync();

            if (!dersler.Any()) return Json(new List<object>());

            var grouped = dersler.GroupBy(d => d.DersGunu)
                    .Select(g => new { gun = g.Key, saatler = g.Select(x => x.DersSaati).OrderBy(s => s).ToList() }).ToList();

            var result = new List<object>();
            foreach (var grup in grouped)
            {
                var saatler = grup.saatler;
                var ardisikGruplar = new List<List<int>>();
                var currentGrup = new List<int> { saatler[0] };

                for (int i = 1; i < saatler.Count; i++)
                {
                    if (saatler[i] == saatler[i - 1] + 1)
                        currentGrup.Add(saatler[i]);
                    else
                    {
                        ardisikGruplar.Add(currentGrup);
                        currentGrup = new List<int> { saatler[i] };
                    }
                }
                ardisikGruplar.Add(currentGrup);

                foreach (var ardisikGrup in ardisikGruplar)
                    result.Add(new { gun = grup.gun, saatler = ardisikGrup });
            }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIkame(int fakulteId, string dersAdi, int kisimNo, int dersSaati,
            int asilHocaId, string dersGunu, int yedekHocaId, int kacSaat, string telafiNedeni, string aciklama)
        {
            try
            {
                var dersProgrami = await _context.DersProgrami.FirstOrDefaultAsync(d =>
                    d.FakulteId == fakulteId && d.KisimNo == kisimNo && d.HocaId == asilHocaId &&
                    d.DersGunu == dersGunu && d.DersSaati == dersSaati);

                if (dersProgrami == null)
                {
                    dersProgrami = new DersProgrami
                    {
                        FakulteId = fakulteId,
                        DersAdi = dersAdi,
                        KisimNo = kisimNo,
                        HocaId = asilHocaId,
                        DersGunu = dersGunu,
                        DersSaati = dersSaati
                    };
                    _context.DersProgrami.Add(dersProgrami);
                    await _context.SaveChangesAsync();
                }

                var yedekHocaDersi = await _context.DersProgrami.AnyAsync(d =>
                    d.HocaId == yedekHocaId && d.DersGunu == dersGunu && d.DersSaati == dersSaati);

                string telafiTuru = yedekHocaDersi ? "Birleþtirme" : "Ýkame";
                var baslangicSaat = TelafiDersService.GetSaatTimeSpan(dersSaati);
                var bitisSaat = TelafiDersService.GetSaatTimeSpan(dersSaati + kacSaat - 1).Add(new TimeSpan(0, 40, 0));

                var telafi = new TelafiDers
                {
                    DersProgramiId = dersProgrami.Id,
                    YedekHocaId = yedekHocaId,
                    FakulteId = fakulteId,
                    TelafiTarihi = DateTime.Today,
                    BaslangicSaat = baslangicSaat,
                    BitisSaat = bitisSaat,
                    TelafiTuru = telafiTuru,
                    TelafiNedeni = telafiNedeni,
                    Aciklama = aciklama,
                    Onaylandi = false
                };

                var result = await _telafiService.CreateTelafiDersAsync(telafi);
                if (result.Success)
                {
                    TempData["Success"] = $"{telafiTuru} kaydý oluþturuldu.";
                    return RedirectToAction(nameof(Index), new { fakulteId });
                }

                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(CreateIkame));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
                return RedirectToAction(nameof(CreateIkame));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateTelafi()
        {
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTelafi(int fakulteId, string dersAdi, int kisimNo, int normalDersSaati,
            int hocaId, string normalGun, DateTime telafiTarihi, TimeSpan baslangicSaat, TimeSpan bitisSaat,
            string telafiNedeni, string aciklama)
        {
            try
            {
                var dersProgrami = await _context.DersProgrami.FirstOrDefaultAsync(d =>
                    d.FakulteId == fakulteId && d.DersAdi == dersAdi && d.KisimNo == kisimNo &&
                    d.HocaId == hocaId && d.DersGunu == normalGun && d.DersSaati == normalDersSaati);

                if (dersProgrami == null)
                {
                    dersProgrami = new DersProgrami
                    {
                        FakulteId = fakulteId,
                        DersAdi = dersAdi,
                        KisimNo = kisimNo,
                        HocaId = hocaId,
                        DersGunu = normalGun,
                        DersSaati = normalDersSaati
                    };
                    _context.DersProgrami.Add(dersProgrami);
                    await _context.SaveChangesAsync();
                }

                var telafi = new TelafiDers
                {
                    DersProgramiId = dersProgrami.Id,
                    YedekHocaId = hocaId,
                    FakulteId = fakulteId,
                    TelafiTarihi = telafiTarihi,
                    BaslangicSaat = baslangicSaat,
                    BitisSaat = bitisSaat,
                    TelafiTuru = "Telafi",
                    TelafiNedeni = telafiNedeni,
                    Aciklama = aciklama,
                    Onaylandi = false
                };

                var result = await _telafiService.CreateTelafiDersAsync(telafi);
                if (result.Success)
                {
                    TempData["Success"] = "Telafi dersi oluþturuldu.";
                    return RedirectToAction(nameof(Index), new { fakulteId });
                }

                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(CreateTelafi));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
                return RedirectToAction(nameof(CreateTelafi));
            }
        }

        public async Task<IActionResult> ExportIkameBirlestirme(int? fakulteId)
        {
            var query = _context.TelafiDersler
                .Include(t => t.DersProgrami).ThenInclude(d => d.Hoca)
                .Include(t => t.YedekHoca).Include(t => t.Fakulte)
                .Where(t => (t.TelafiTuru == "Ýkame" || t.TelafiTuru == "Birleþtirme") && !t.CiktiAlindi);

            if (fakulteId.HasValue)
                query = query.Where(t => t.FakulteId == fakulteId.Value);

            var telafiler = await query.OrderBy(t => t.TelafiTarihi)
                .ThenBy(t => t.FakulteId).ThenBy(t => t.DersProgrami.DersAdi).ToListAsync();

            if (!telafiler.Any())
            {
                TempData["Error"] = "Çýktý alýnacak Ýkame/Birleþtirme kaydý bulunamadý.";
                return RedirectToAction(nameof(Index), new { fakulteId });
            }

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Ýkame-Birleþtirme Listesi");

            var headers = new[]
            {
                "S. NU.", "DERS ADI", "SINIF", "BÝRLEÞTÝRÝLEN KISIMLAR", "ÝKAME EDÝLEN KISIM",
                "DERS SAATÝ", "TOPLAM ÝKAME/BÝRLEÞTÝRME YAPILAN SAAT", "DERS ÖÐRETMENÝ",
                "GÖREVLENDÝRÝLEN ÖÐRETMEN", "DERSÝN NE ÞEKÝLDE ÝÞLENECEÐÝ",
                "ÝKAME / BÝRLEÞTÝRME SEBEBÝ", "DERSÝN ÝÞLENECEÐÝ YER", "DERSÝN TARÝHÝ"
            };

            ws.Row(1).Height = 100;

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 18;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                cell.Style.WrapText = true;
            }

            ws.Column(1).Width = 8;
            ws.Column(2).Width = 30;
            ws.Column(3).Width = 15;
            ws.Column(4).Width = 18;
            ws.Column(5).Width = 15;
            ws.Column(6).Width = 20;
            ws.Column(7).Width = 18;
            ws.Column(8).Width = 25;
            ws.Column(9).Width = 25;
            ws.Column(10).Width = 18;
            ws.Column(11).Width = 24;
            ws.Column(12).Width = 20;
            ws.Column(13).Width = 22;

            ws.View.FreezePanes(2, 1);

            int row = 2;
            int siraNo = 1;

            var ikameFill = Color.FromArgb(173, 216, 230);
            var ikameBorder = Color.FromArgb(49, 106, 197);
            var birlestirmeFill = Color.FromArgb(255, 224, 178);
            var birlestirmeBorder = Color.FromArgb(237, 125, 49);
            var tarihRenk = Color.FromArgb(0, 176, 240);

            foreach (var telafi in telafiler)
            {
                ws.Row(row).Height = 100;

                var saatGosterim = GetSaatGosterim(telafi.BaslangicSaat, telafi.BitisSaat);
                var toplamSaat = GetToplamSaat(telafi.BaslangicSaat, telafi.BitisSaat);
                var sinif = $"{telafi.Fakulte.Ad} {telafi.DersProgrami?.KisimNo}";

                string birlestirilenKisimlar = "";
                string ikameEdilenKisim = "";
                string dersinYeri = "";

                if (telafi.TelafiTuru == "Birleþtirme")
                {
                    var yedekHocaKisim = await _context.DersProgrami
                        .Where(d => d.HocaId == telafi.YedekHocaId &&
                            d.DersGunu == telafi.DersProgrami.DersGunu &&
                            d.DersSaati == telafi.DersProgrami.DersSaati)
                        .Select(d => d.KisimNo).FirstOrDefaultAsync();

                    birlestirilenKisimlar = $"{telafi.DersProgrami?.KisimNo}-{yedekHocaKisim}";
                    ikameEdilenKisim = telafi.DersProgrami?.KisimNo.ToString();
                    dersinYeri = "";
                }
                else
                {
                    ikameEdilenKisim = telafi.DersProgrami?.KisimNo.ToString();
                    dersinYeri = $"{telafi.DersProgrami?.KisimNo}. KISIM";
                }

                ws.Cells[row, 1].Value = siraNo;
                ws.Cells[row, 2].Value = telafi.DersProgrami?.DersAdi;
                ws.Cells[row, 3].Value = sinif;
                ws.Cells[row, 4].Value = birlestirilenKisimlar;
                ws.Cells[row, 5].Value = ikameEdilenKisim;
                ws.Cells[row, 6].Value = saatGosterim;
                ws.Cells[row, 7].Value = toplamSaat;
                ws.Cells[row, 8].Value = telafi.DersProgrami?.Hoca != null
                    ? $"{telafi.DersProgrami.Hoca.Rutbe} {telafi.DersProgrami.Hoca.AdSoyad}" : "";
                ws.Cells[row, 9].Value = telafi.YedekHoca != null
                    ? $"{telafi.YedekHoca.Rutbe} {telafi.YedekHoca.AdSoyad}" : "";
                ws.Cells[row, 10].Value = telafi.TelafiTuru?.ToUpper();
                ws.Cells[row, 11].Value = telafi.TelafiNedeni;
                ws.Cells[row, 12].Value = dersinYeri;
                ws.Cells[row, 13].Value = telafi.TelafiTarihi.ToString("dd MMMM yyyy dddd",
                    new System.Globalization.CultureInfo("tr-TR"));

                for (int c = 1; c <= headers.Length; c++)
                {
                    var cell = ws.Cells[row, c];
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.WrapText = true;
                    cell.Style.Font.Size = 18;
                    cell.Style.Font.Bold = true;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                }

                var turCell = ws.Cells[row, 10];
                if (telafi.TelafiTuru == "Ýkame")
                {
                    turCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    turCell.Style.Fill.BackgroundColor.SetColor(ikameFill);
                    turCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ikameBorder);
                }
                else if (telafi.TelafiTuru == "Birleþtirme")
                {
                    turCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    turCell.Style.Fill.BackgroundColor.SetColor(birlestirmeFill);
                    turCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, birlestirmeBorder);
                }

                var tarihCell = ws.Cells[row, 13];
                tarihCell.Style.Fill.BackgroundColor.SetColor(tarihRenk);

                row++;
                siraNo++;
            }

            ws.Cells[1, 1, row - 1, headers.Length].Style.Font.Name = "Calibri";

            foreach (var telafi in telafiler)
                telafi.CiktiAlindi = true;
            await _context.SaveChangesAsync();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Ikame_Birlestirme_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private string GetSaatGosterim(TimeSpan baslangic, TimeSpan bitis)
        {
            int baslangicSaat = 1;
            for (int i = 1; i <= 8; i++)
            {
                if (TelafiDersService.GetSaatTimeSpan(i) == baslangic)
                {
                    baslangicSaat = i;
                    break;
                }
            }

            int bitisSaat = baslangicSaat;
            for (int i = baslangicSaat; i <= 8; i++)
            {
                var sonraki = TelafiDersService.GetSaatTimeSpan(i).Add(new TimeSpan(0, 40, 0));
                if (sonraki >= bitis)
                {
                    bitisSaat = i;
                    break;
                }
            }
            return baslangicSaat == bitisSaat ? $"{baslangicSaat}. DERS" : $"{baslangicSaat}-{bitisSaat}. DERSLER";
        }

        private string GetToplamSaat(TimeSpan baslangic, TimeSpan bitis)
        {
            int baslangicSaat = 1;
            for (int i = 1; i <= 8; i++)
            {
                if (TelafiDersService.GetSaatTimeSpan(i) == baslangic)
                {
                    baslangicSaat = i;
                    break;
                }
            }

            int bitisSaat = baslangicSaat;
            for (int i = baslangicSaat; i <= 8; i++)
            {
                var sonraki = TelafiDersService.GetSaatTimeSpan(i).Add(new TimeSpan(0, 40, 0));
                if (sonraki >= bitis)
                {
                    bitisSaat = i;
                    break;
                }
            }

            int toplamDers = bitisSaat - baslangicSaat + 1;
            return $"({toplamDers}) DERS SAATÝ";
        }

        [HttpGet]
        public async Task<IActionResult> KontrolEt(int fakulteId, int yedekHocaId, string gun, int saat)
        {
            var varMi = await _context.DersProgrami.AnyAsync(d =>
                d.FakulteId == fakulteId && d.HocaId == yedekHocaId && d.DersGunu == gun && d.DersSaati == saat);
            return Json(new { varMi });
        }

        public async Task<IActionResult> Details(int id)
        {
            var telafi = await _telafiService.GetTelafiDersAsync(id);
            return telafi == null ? NotFound() : View(telafi);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var telafi = await _telafiService.GetTelafiDersAsync(id);
            if (telafi == null)
            {
                TempData["Error"] = "Kayýt bulunamadý.";
                return RedirectToAction(nameof(Index));
            }

            var oneri = await _telafiService.GetTelafiOneriAsync(
                telafi.DersProgramiId, telafi.TelafiTarihi, telafi.BaslangicSaat, telafi.BitisSaat);

            ViewBag.TelafiId = id;
            ViewBag.MevcutTelafi = telafi;
            return View(oneri);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TelafiDers telafiDers)
        {
            if (id != telafiDers.Id)
            {
                TempData["Error"] = "Geçersiz istek.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _telafiService.UpdateTelafiDersAsync(telafiDers);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Onayla(int id)
        {
            var result = await _telafiService.OnaylaTelafiDersAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _telafiService.DeleteTelafiDersAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ? TOPLU TELAFÝ METODLARI
        [HttpGet]
        public async Task<IActionResult> TopluTelafiOlustur()
        {
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
            ViewBag.Gunler = new List<string> { "Pazartesi", "Salý", "Çarþamba", "Perþembe", "Cuma" };
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluTelafiOlustur(
            int fakulteId,
            string telafiYapilamayacakGun,
            DateTime telafiTarihi,
            string telafiNedeni)
        {
            try
            {
                var ayarlar = await _context.TaburTelafiAyarlari
                    .Where(t => t.Tabur.FakulteId == fakulteId).FirstOrDefaultAsync();

                if (ayarlar == null)
                {
                    TempData["Error"] = "Bu fakülte için telafi ayarlarý bulunamadý. Önce ayarlarý yapýn.";
                    return RedirectToAction(nameof(TopluTelafiOlustur));
                }

                var sonuc = await _telafiService.TopluTelafiOlusturAsync(ayarlar.TaburId, telafiYapilamayacakGun, telafiTarihi, telafiNedeni);

                if (sonuc.Success)
                {
                    TempData["Success"] = $"? Toplu telafi oluþturuldu!\n\n" +
                        $"Baþarýlý: {sonuc.Basarili}\n" +
                        $"Baþarýsýz: {sonuc.Basarisiz}\n\n" +
                        $"{sonuc.Message}";
                }
                else
                {
                    TempData["Error"] = sonuc.Message;
                }

                return RedirectToAction(nameof(Index), new { fakulteId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
                return RedirectToAction(nameof(TopluTelafiOlustur));
            }
        }
    }
}
