using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; // EPPlus Kütüphanesi
using System.Text;
using System.Text.RegularExpressions;

namespace Etutlist.Controllers
{
    public class DersProgramiController : Controller
    {
        private readonly AppDbContext _context;

        // Excel Sütun Başlangıç İndeksi (1 tabanlı)
        // 1: S.NO, 2: DERS, 3: HOCA, 4: D/S, 5: İlk Ders Saati (Pzt 1)
        private const int EXCEL_ILKSAAT_SUTUN = 5;

        public DersProgramiController(AppDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------
        // ANA EKRAN (INDEX)
        // -------------------------------------------------------------------
        public async Task<IActionResult> Index(int? fakulteId)
        {
            if (!fakulteId.HasValue)
            {
                var ilkFakulte = await _context.Fakulteler.FirstOrDefaultAsync();
                fakulteId = ilkFakulte?.Id ?? 0;
            }

            ViewBag.SelectedFakulteId = fakulteId;
            ViewBag.Fakulteler = await _context.Fakulteler.ToListAsync();

            if (fakulteId == 0) return View(new List<DersGrupViewModel>());

            var hocaDersler = await _context.HocaDersler
                .Include(hd => hd.Hoca)
                .Include(hd => hd.Ders)
                .Where(hd => hd.Hoca.FakulteId == fakulteId && hd.Hoca.AktifMi)
                .ToListAsync();

            var mevcutDersler = await _context.DersProgrami
                .Include(d => d.Hoca)
                .Include(d => d.Ders)
                .Where(d => d.FakulteId == fakulteId)
                .ToListAsync();

            var dersGruplari = hocaDersler
                .GroupBy(hd => hd.Ders.DersAdi)
                .Select(g => new DersGrupViewModel
                {
                    DersAdi = g.Key,
                    DersId = g.First().DersId,
                    Hocalar = g.Select(hd => new HocaDersViewModel
                    {
                        HocaId = hd.HocaId,
                        HocaAdi = hd.Hoca.AdSoyad,
                        DersAdi = hd.Ders.DersAdi,
                        DersSaatleri = mevcutDersler
                            .Where(d => d.HocaId == hd.HocaId && d.DersAdi == hd.Ders.DersAdi)
                            .ToList()
                    }).ToList()
                }).ToList();

            return View(dersGruplari);
        }

        // -------------------------------------------------------------------
        // EXCEL YÜKLEME (.xlsx) - AKILLI MOD (DB AYARI veya OTOMATİK)
        // -------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> UploadExcel()
        {
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(int fakulteId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Lütfen bir Excel dosyası seçin!";
                return RedirectToAction(nameof(UploadExcel));
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                // 1. FAKÜLTE SAAT AYARLARINI GETİR
                // Önce veritabanına bak, orada ayar varsa onu kullan.
                // Yoksa Excel'i analiz edip otomatik bulmaya çalışacak.
                var fakulte = await _context.Fakulteler.FindAsync(fakulteId);
                int[] saatAyarlari;

                if (!string.IsNullOrEmpty(fakulte?.GunlukDersSaatleri))
                {
                    // DB'de ayar var (Örn: "7,7,6,7,4") -> Bunu kullan
                    saatAyarlari = fakulte.SaatlerDizisi;
                }
                else
                {
                    // DB'de ayar yok -> Dosyayı geçici açıp otomatik algıla
                    using (var tempStream = new MemoryStream())
                    {
                        await file.CopyToAsync(tempStream);
                        using (var tempPackage = new ExcelPackage(tempStream))
                        {
                            var tempSheet = tempPackage.Workbook.Worksheets.FirstOrDefault();
                            // Eğer sayfa boşsa varsayılanı döndür, değilse algıla
                            saatAyarlari = (tempSheet?.Dimension != null)
                                ? DetectGunlukDersSayilari(tempSheet)
                                : new int[] { 9, 9, 9, 9, 9 };
                        }
                    }
                    // Dosya stream'ini başa sar (Tekrar okumak için)
                    file.OpenReadStream().Position = 0;
                }

                int addedCount = 0, skippedCount = 0, hocaCount = 0, dersCount = 0;

                // Mükerrer kontrolü için veritabanındaki mevcut dersleri hafızaya al
                var eklenenDerslerCache = new HashSet<string>();
                var dbDersler = await _context.DersProgrami
                    .Where(d => d.FakulteId == fakulteId)
                    .Select(d => new { d.KisimNo, d.DersGunu, d.DersSaati })
                    .ToListAsync();

                foreach (var d in dbDersler)
                    eklenenDerslerCache.Add($"{d.KisimNo}-{d.DersGunu}-{d.DersSaati}");

                // 2. EXCEL DOSYASINI İŞLE
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["Error"] = "Excel dosyasında çalışma sayfası bulunamadı.";
                            return RedirectToAction(nameof(UploadExcel));
                        }

                        if (worksheet.Dimension == null)
                        {
                            TempData["Error"] = "Yüklenen Excel sayfasında veri bulunamadı.";
                            return RedirectToAction(nameof(UploadExcel));
                        }

                        int rowCount = worksheet.Dimension.Rows;
                        string currentDersAdi = "";

                        var gunler = new[] { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };

                        // 3. Satırdan başla (1 ve 2 başlık satırları)
                        for (int row = 3; row <= rowCount; row++)
                        {
                            // 1. SÜTUN: S.NO Kontrolü (Dipnot satırlarını atlamak için)
                            var sNoVal = worksheet.Cells[row, 1].Text;
                            if (!int.TryParse(sNoVal, out _)) continue;

                            // 2. SÜTUN: DERS ADI (Merge Mantığı)
                            var cellDersAdi = worksheet.Cells[row, 2].Text?.Trim();
                            if (!string.IsNullOrWhiteSpace(cellDersAdi))
                            {
                                currentDersAdi = cellDersAdi;
                                // Ders yoksa ekle
                                var dersKaydi = await _context.Dersler.FirstOrDefaultAsync(d => d.DersAdi == currentDersAdi);
                                if (dersKaydi == null)
                                {
                                    dersKaydi = new Ders { DersAdi = currentDersAdi };
                                    _context.Dersler.Add(dersKaydi);
                                    await _context.SaveChangesAsync();
                                    dersCount++;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(currentDersAdi)) continue;

                            // 3. SÜTUN: HOCA BİLGİSİ
                            var hocaBilgi = worksheet.Cells[row, 3].Text?.Trim();
                            if (string.IsNullOrWhiteSpace(hocaBilgi)) continue;

                            var hoca = await FindOrCreateHocaAsync(fakulteId, hocaBilgi);

                            // Hoca-Ders İlişkisi
                            var bulunanDers = await _context.Dersler.FirstOrDefaultAsync(d => d.DersAdi == currentDersAdi);
                            if (bulunanDers != null)
                            {
                                var hocaDers = await _context.HocaDersler
                                    .FirstOrDefaultAsync(hd => hd.HocaId == hoca.Id && hd.DersId == bulunanDers.Id);

                                if (hocaDers == null)
                                {
                                    _context.HocaDersler.Add(new HocaDers { HocaId = hoca.Id, DersId = bulunanDers.Id });
                                    await _context.SaveChangesAsync();
                                    hocaCount++;
                                }
                            }

                            // 4. SAATLERİ TARAMA
                            int currentColumn = EXCEL_ILKSAAT_SUTUN;

                            for (int gunIndex = 0; gunIndex < 5; gunIndex++)
                            {
                                var gun = gunler[gunIndex];
                                // BURADA AYARLANAN SAATİ KULLANIYORUZ
                                int maxSaat = saatAyarlari[gunIndex];

                                for (int saat = 1; saat <= maxSaat; saat++)
                                {
                                    var cell = worksheet.Cells[row, currentColumn];
                                    var kisimNoRaw = cell.Text?.Trim();

                                    // BİRLEŞİK HÜCRE (MERGE) KONTROLÜ
                                    if (string.IsNullOrEmpty(kisimNoRaw) && cell.Merge)
                                    {
                                        var mergeId = worksheet.GetMergeCellId(row, currentColumn);
                                        if (mergeId > 0)
                                        {
                                            var mergeAddress = worksheet.MergedCells[mergeId - 1];
                                            var startAddress = mergeAddress.Split(':')[0];
                                            kisimNoRaw = worksheet.Cells[startAddress].Text?.Trim();
                                        }
                                    }

                                    currentColumn++;

                                    if (string.IsNullOrWhiteSpace(kisimNoRaw)) continue;

                                    // ÇOKLU KISIM AYIRMA (Örn: "4-29-37")
                                    var kisimlar = new List<int>();
                                    var parcalar = kisimNoRaw.Split(new[] { '-', ' ', '+', ',' }, StringSplitOptions.RemoveEmptyEntries);

                                    foreach (var parca in parcalar)
                                    {
                                        if (int.TryParse(parca.Trim(), out int k))
                                            kisimlar.Add(k);
                                    }

                                    foreach (var kisim in kisimlar)
                                    {
                                        string cacheKey = $"{kisim}-{gun}-{saat}";

                                        if (eklenenDerslerCache.Contains(cacheKey))
                                        {
                                            skippedCount++;
                                            continue;
                                        }

                                        var dersProgrami = new DersProgrami
                                        {
                                            FakulteId = fakulteId,
                                            HocaId = hoca.Id,
                                            DersId = bulunanDers!.Id,
                                            DersAdi = currentDersAdi,
                                            KisimNo = kisim,
                                            DersGunu = gun,
                                            DersSaati = saat,
                                            DersKodu = ""
                                        };

                                        _context.DersProgrami.Add(dersProgrami);
                                        eklenenDerslerCache.Add(cacheKey);
                                        addedCount++;
                                    }
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"✅ İşlem Başarılı!\n" +
                                      $"- {addedCount} ders saati eklendi.\n" +
                                      $"- {dersCount} yeni ders tanımlandı.\n" +
                                      $"- {hocaCount} yeni hoca ilişkisi kuruldu.";

                if (skippedCount > 0)
                    TempData["Warning"] = $"⚠️ {skippedCount} ders çakışma nedeniyle atlandı.";

                return RedirectToAction(nameof(Index), new { fakulteId });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                TempData["Error"] = "Hata: " + msg;
                ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
                return View();
            }
        }

        // -------------------------------------------------------------------
        // YARDIMCI METODLAR
        // -------------------------------------------------------------------

        // Excel'den Otomatik Günlük Ders Saatlerini Algıla
        private int[] DetectGunlukDersSayilari(ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension == null) return new[] { 9, 9, 9, 9, 9 };

            var counts = new int[] { 0, 0, 0, 0, 0 };
            var gunler = new[] { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };

            int colCount = worksheet.Dimension.Columns;
            int currentDayIndex = -1;

            // 5. Sütundan (İlk Ders) başlayarak başlıkları tara
            for (int col = EXCEL_ILKSAAT_SUTUN; col <= colCount; col++)
            {
                // 1. Satır: GÜN ADI
                var headerDay = worksheet.Cells[1, col].Text?.Trim();

                for (int i = 0; i < 5; i++)
                {
                    if (!string.IsNullOrEmpty(headerDay) &&
                        headerDay.Contains(gunler[i], StringComparison.OrdinalIgnoreCase))
                    {
                        currentDayIndex = i;
                        break;
                    }
                }

                // 2. Satır: SAAT NUMARASI
                var subHeader = worksheet.Cells[2, col].Text?.Trim();
                if (!string.IsNullOrEmpty(subHeader) && currentDayIndex >= 0 && currentDayIndex < 5)
                {
                    counts[currentDayIndex]++;
                }
            }

            // Güvenlik: Bulamazsa 9 döndür
            for (int i = 0; i < 5; i++) if (counts[i] == 0) counts[i] = 9;

            return counts;
        }

        private async Task<Hoca> FindOrCreateHocaAsync(int fakulteId, string adSoyad)
        {
            adSoyad = adSoyad.Replace("\"", "").Trim();
            if (adSoyad.Length > 100) adSoyad = adSoyad.Substring(0, 100);

            var hoca = await _context.Hocalar
                .FirstOrDefaultAsync(h => h.AdSoyad == adSoyad && h.FakulteId == fakulteId);

            if (hoca != null) return hoca;

            string rutbe = "";
            string isim = adSoyad;

            var match = Regex.Match(adSoyad, @"^([A-ZÇĞİÖŞÜa-zçğıöşü]+\.)+");
            if (match.Success)
            {
                rutbe = match.Value.TrimEnd();
                isim = adSoyad.Substring(match.Length).Trim();
            }
            else
            {
                var parts = adSoyad.Split(new[] { ' ' }, 2);
                if (parts.Length > 1 && (parts[0].Contains('.') || parts[0].Length <= 5))
                {
                    rutbe = parts[0];
                    isim = parts[1];
                }
            }

            if (rutbe.Length > 50) rutbe = rutbe.Substring(0, 50);
            if (isim.Length > 100) isim = isim.Substring(0, 100);

            hoca = new Hoca
            {
                FakulteId = fakulteId,
                Rutbe = rutbe,
                AdSoyad = isim,
                AktifMi = true
            };

            _context.Hocalar.Add(hoca);
            await _context.SaveChangesAsync();

            return hoca;
        }

        // Tekil Ekleme (Manuel)
        [HttpPost]
        public async Task<IActionResult> AddDers([FromBody] AddDersRequest request)
        {
            try
            {
                var mevcutDers = await _context.DersProgrami.AnyAsync(d =>
                    d.FakulteId == request.FakulteId && d.KisimNo == request.KisimNo &&
                    d.DersGunu == request.Gun && d.DersSaati == request.Saat);

                if (mevcutDers) return Json(new { success = false, message = "Bu saat dolu!" });

                var hoca = await _context.Hocalar.FindAsync(request.HocaId);
                var hocaDers = await _context.HocaDersler.Include(hd => hd.Ders)
                    .FirstOrDefaultAsync(hd => hd.HocaId == request.HocaId && hd.DersId == request.DersId);

                if (hoca == null || hocaDers == null) return Json(new { success = false, message = "Veri hatası!" });

                var ders = new DersProgrami
                {
                    FakulteId = request.FakulteId,
                    KisimNo = request.KisimNo,
                    HocaId = request.HocaId,
                    DersId = request.DersId,
                    DersAdi = hocaDers.Ders.DersAdi,
                    DersGunu = request.Gun,
                    DersSaati = request.Saat
                };

                _context.DersProgrami.Add(ders);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Eklendi", dersId = ders.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // Ders Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DersSil(int id, int? fakulteId)
        {
            var ders = await _context.DersProgrami.FindAsync(id);
            if (ders != null)
            {
                _context.DersProgrami.Remove(ders);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ders silindi.";
            }
            return RedirectToAction(nameof(Index), new { fakulteId });
        }

        // Autocomplete
        [HttpGet]
        public IActionResult SearchHocalar(int fakulteId, string term, int? dersId)
        {
            var query = _context.Hocalar.Where(h => h.AktifMi && h.FakulteId == fakulteId && h.AdSoyad.Contains(term));
            if (dersId.HasValue && dersId > 0)
                query = query.Where(h => h.HocaDersler.Any(hd => hd.DersId == dersId));

            return Json(query.Select(h => new { id = h.Id, adSoyad = h.AdSoyad, rutbe = h.Rutbe }).Take(10).ToList());
        }

        [HttpGet]
        public IActionResult SearchDersler(string term)
        {
            return Json(_context.Dersler.Where(d => d.DersAdi.Contains(term))
                .Select(d => new { id = d.Id, dersAdi = d.DersAdi }).Take(20).ToList());
        }

        // Toplu Ekleme (Manuel)
        [HttpGet]
        public async Task<IActionResult> BulkAdd()
        {
            ViewBag.Fakulteler = new SelectList(await _context.Fakulteler.ToListAsync(), "Id", "Ad");
            ViewBag.Gunler = new List<string> { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAdd(BulkAddViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }
    }

    // --- MODELLER ---
    public class AddDersRequest
    {
        public int FakulteId { get; set; }
        public int HocaId { get; set; }
        public int DersId { get; set; }
        public int KisimNo { get; set; }
        public string Gun { get; set; }
        public int Saat { get; set; }
    }

    public class BulkAddViewModel
    {
        public int FakulteId { get; set; }
        public int? DersId { get; set; }
        public List<DersItemViewModel> Dersler { get; set; } = new();
    }

    public class DersItemViewModel
    {
        public bool Selected { get; set; }
        public string DersKodu { get; set; }
        public string DersAdi { get; set; }
        public int KisimNo { get; set; }
        public string HocaAdi { get; set; }
        public string Gun { get; set; }
        public int Saat { get; set; }
    }

    public class DersGrupViewModel
    {
        public string DersAdi { get; set; }
        public int DersId { get; set; }
        public List<HocaDersViewModel> Hocalar { get; set; }
    }

    public class HocaDersViewModel
    {
        public int HocaId { get; set; }
        public string HocaAdi { get; set; }
        public string DersAdi { get; set; }
        public List<DersProgrami> DersSaatleri { get; set; }
    }
}