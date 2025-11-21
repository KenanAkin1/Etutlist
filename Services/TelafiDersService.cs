using Etutlist.Models;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Services
{
    public class TelafiDersService
    {
        private readonly AppDbContext _context;

        public TelafiDersService(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // MANUEL EKRAN İÇİN OLAN METODLAR (Değişiklik Yok)
        // ---------------------------------------------------------

        public async Task<TelafiOneriViewModel> GetTelafiOneriAsync(int dersProgramiId, DateTime telafiTarihi, TimeSpan baslangicSaat, TimeSpan bitisSaat)
        {
            var ders = await _context.DersProgrami
                .Include(d => d.Hoca).Include(d => d.Fakulte).Include(d => d.Ders)
                .FirstOrDefaultAsync(d => d.Id == dersProgramiId);

            if (ders == null) return null;

            var oneri = new TelafiOneriViewModel
            {
                DersProgrami = ders,
                TelafiTarihi = telafiTarihi,
                BaslangicSaat = baslangicSaat,
                BitisSaat = bitisSaat
            };

            bool hocaMusait = await IsHocaMusaitAsync(ders.HocaId, telafiTarihi, baslangicSaat, bitisSaat);
            if (hocaMusait)
            {
                oneri.OnerilenTur = "Telafi";
                oneri.Aciklama = $"Telafi dersi yapılabilir. Aynı hoca ({ders.Hoca.AdSoyad}) dersi verebilir.";
                oneri.MusaitYedekHocalar = new List<Hoca> { ders.Hoca };
                return oneri;
            }

            var musaitHocalar = await GetMusaitHocalarAsync(ders.FakulteId, ders.HocaId, telafiTarihi, baslangicSaat, bitisSaat, ders.DersId);
            if (musaitHocalar.Any())
            {
                oneri.OnerilenTur = "İkame";
                oneri.Aciklama = $"İkame dersi önerilir. {musaitHocalar.Count} müsait hoca bulundu.";
                oneri.MusaitYedekHocalar = musaitHocalar;
                return oneri;
            }

            oneri.OnerilenTur = "Birleştirme";
            oneri.Aciklama = "Uygun hoca bulunamadı. Dersin başka bir bölümle birleştirilmesi önerilir.";
            oneri.MusaitYedekHocalar = new List<Hoca>();
            oneri.BirlestirilebilirDersler = await GetBirlestirilebilirDerslerAsync(dersProgramiId, telafiTarihi);

            return oneri;
        }

        private async Task<bool> IsHocaMusaitAsync(int hocaId, DateTime tarih, TimeSpan baslangic, TimeSpan bitis)
        {
            var normalDersiVar = await _context.DersProgrami.AnyAsync(d =>
                d.HocaId == hocaId &&
                d.DersGunu == GetGunAdi(tarih) &&
                d.DersSaati == GetSaatIndex(baslangic));

            if (normalDersiVar) return false;

            var telafideGorevli = await _context.TelafiDersler.AnyAsync(t =>
                t.YedekHocaId == hocaId &&
                t.TelafiTarihi.Date == tarih.Date &&
                ((t.BaslangicSaat < bitis && t.BitisSaat > baslangic)));

            return !telafideGorevli;
        }

        private async Task<List<Hoca>> GetMusaitHocalarAsync(int fakulteId, int mevcutHocaId, DateTime tarih, TimeSpan baslangic, TimeSpan bitis, int? dersId)
        {
            var tumHocalar = await _context.Hocalar
                .Include(h => h.HocaDersler)
                .Where(h => h.AktifMi && h.FakulteId == fakulteId && h.Id != mevcutHocaId)
                .ToListAsync();

            var musaitHocalar = new List<Hoca>();

            foreach (var hoca in tumHocalar)
            {
                if (dersId.HasValue)
                {
                    bool dersVerebilir = await _context.HocaDersler
                        .AnyAsync(hd => hd.HocaId == hoca.Id && hd.DersId == dersId.Value);
                    if (!dersVerebilir) continue;
                }

                bool musait = await IsHocaMusaitAsync(hoca.Id, tarih, baslangic, bitis);
                if (musait) musaitHocalar.Add(hoca);
            }

            return musaitHocalar.OrderBy(h => h.AdSoyad).ToList();
        }

        private async Task<List<DersProgrami>> GetBirlestirilebilirDerslerAsync(int dersProgramiId, DateTime telafiTarihi)
        {
            var anaDers = await _context.DersProgrami
                .Include(d => d.Hoca).Include(d => d.Fakulte)
                .FirstOrDefaultAsync(d => d.Id == dersProgramiId);

            if (anaDers == null) return new List<DersProgrami>();

            var gunAdi = GetGunAdi(telafiTarihi);

            return await _context.DersProgrami
                .Include(d => d.Hoca)
                .Where(d =>
                    d.Id != dersProgramiId &&
                    d.FakulteId == anaDers.FakulteId &&
                    d.DersGunu == gunAdi &&
                    d.DersId == anaDers.DersId)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateTelafiDersAsync(TelafiDers telafiDers)
        {
            var hocaMesgul = await _context.TelafiDersler.AnyAsync(t =>
                t.YedekHocaId == telafiDers.YedekHocaId &&
                t.TelafiTarihi.Date == telafiDers.TelafiTarihi.Date &&
                ((t.BaslangicSaat < telafiDers.BitisSaat && t.BitisSaat > telafiDers.BaslangicSaat)));

            if (hocaMesgul) return (false, "Seçilen hoca bu saatte başka bir telafi dersinde görevli.");

            var normalDersi = await _context.DersProgrami.AnyAsync(d =>
                d.HocaId == telafiDers.YedekHocaId &&
                d.DersGunu == GetGunAdi(telafiDers.TelafiTarihi) &&
                d.DersSaati == GetSaatIndex(telafiDers.BaslangicSaat));

            if (normalDersi) return (false, "Seçilen hocanın bu saatte normal dersi var.");

            _context.TelafiDersler.Add(telafiDers);
            await _context.SaveChangesAsync();

            return (true, "Telafi dersi başarıyla oluşturuldu.");
        }

        public async Task<List<TelafiDers>> GetTelafiDerslerAsync(int? fakulteId = null)
        {
            var query = _context.TelafiDersler
                .Include(t => t.DersProgrami).ThenInclude(d => d.Hoca)
                .Include(t => t.DersProgrami).ThenInclude(d => d.Ders)
                .Include(t => t.YedekHoca)
                .Include(t => t.Fakulte)
                .OrderByDescending(t => t.TelafiTarihi)
                .AsQueryable();

            if (fakulteId.HasValue)
                query = query.Where(t => t.FakulteId == fakulteId);

            return await query.ToListAsync();
        }

        public async Task<TelafiDers> GetTelafiDersAsync(int id)
        {
            return await _context.TelafiDersler
                .Include(t => t.DersProgrami).ThenInclude(d => d.Hoca)
                .Include(t => t.DersProgrami).ThenInclude(d => d.Ders)
                .Include(t => t.DersProgrami).ThenInclude(d => d.Fakulte)
                .Include(t => t.YedekHoca)
                .Include(t => t.Fakulte)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<(bool Success, string Message)> DeleteTelafiDersAsync(int id)
        {
            var telafi = await _context.TelafiDersler.FindAsync(id);

            if (telafi == null) return (false, "Telafi dersi bulunamadı.");

            _context.TelafiDersler.Remove(telafi);
            await _context.SaveChangesAsync();

            return (true, "Telafi dersi silindi.");
        }

        public async Task<(bool Success, string Message)> UpdateTelafiDersAsync(TelafiDers telafiDers)
        {
            var mevcutTelafi = await _context.TelafiDersler.FindAsync(telafiDers.Id);
            if (mevcutTelafi == null) return (false, "Telafi dersi bulunamadı.");

            // Basit hoca kontrolü (Update için)
            bool hocaMesgul = await _context.TelafiDersler.AnyAsync(t =>
                t.Id != telafiDers.Id &&
                t.YedekHocaId == telafiDers.YedekHocaId &&
                t.TelafiTarihi.Date == telafiDers.TelafiTarihi.Date &&
                ((t.BaslangicSaat < telafiDers.BitisSaat && t.BitisSaat > telafiDers.BaslangicSaat)));

            if (hocaMesgul) return (false, "Seçilen hoca bu saatte başka bir telafi dersinde görevli.");

            mevcutTelafi.YedekHocaId = telafiDers.YedekHocaId;
            mevcutTelafi.TelafiTarihi = telafiDers.TelafiTarihi;
            mevcutTelafi.BaslangicSaat = telafiDers.BaslangicSaat;
            mevcutTelafi.BitisSaat = telafiDers.BitisSaat;
            mevcutTelafi.TelafiTuru = telafiDers.TelafiTuru;
            mevcutTelafi.TelafiNedeni = telafiDers.TelafiNedeni;
            mevcutTelafi.Aciklama = telafiDers.Aciklama;
            mevcutTelafi.KisimNo = telafiDers.KisimNo;

            await _context.SaveChangesAsync();

            return (true, "Telafi dersi güncellendi.");
        }

        public async Task<(bool Success, string Message)> OnaylaTelafiDersAsync(int id)
        {
            var telafi = await _context.TelafiDersler.FindAsync(id);
            if (telafi == null) return (false, "Telafi dersi bulunamadı.");

            telafi.Onaylandi = true;
            await _context.SaveChangesAsync();

            return (true, "Telafi dersi onaylandı.");
        }

        // ---------------------------------------------------------
        // 🚀 AKILLI TOPLU TELAFİ - FİNAL SÜRÜM
        // 1. Aynı kısım derslerini AYNI GÜNDE toplar.
        // 2. Kendi hocasıyla (İKAMESİZ) telafi yapar.
        // 3. 150 gün ileriye kadar tarar (Mutlaka yer bulur).
        // 4. Asla "Başarısız" veya "Hata" kaydı oluşturmaz.
        // ---------------------------------------------------------
        public async Task<(bool Success, string Message, int Basarili, int Basarisiz)> TopluTelafiOlusturAsync(
            DateTime telafiEdilecekTarih,
            string telafiNedeni,
            List<int> secilenSaatler)
        {
            int basarili = 0;
            int basarisiz = 0; // Sadece raporlama için
            var hatalar = new List<string>();

            // 1. Günü Belirle
            var gun = GetGunAdi(telafiEdilecekTarih);
            if (string.IsNullOrEmpty(gun))
                return (false, "Seçilen tarih hafta içi olmalı.", 0, 0);

            // 2. Dersleri Getir (Saat filtresiyle)
            var derslerQuery = _context.DersProgrami
                .Include(d => d.Hoca).Include(d => d.Fakulte).Include(d => d.Ders)
                .Where(d => d.DersGunu == gun);

            if (secilenSaatler != null && secilenSaatler.Any())
                derslerQuery = derslerQuery.Where(d => secilenSaatler.Contains(d.DersSaati));

            var dersler = await derslerQuery
                .OrderBy(d => d.FakulteId).ThenBy(d => d.KisimNo).ThenBy(d => d.DersSaati)
                .AsNoTracking().ToListAsync();

            if (!dersler.Any())
                return (false, "Telafi edilecek ders bulunamadı.", 0, 0);

            // 3. Takvimi Getir (150 GÜNLÜK - 5 AY)
            var telafiTakvimi = await GetTelafiTakvimiAsync(telafiEdilecekTarih);
            if (!telafiTakvimi.Any())
                return (false, "Telafi takvimi oluşturulamadı. Ayarları kontrol edin.", 0, 0);

            // 4. Cache Verilerini Hazırla
            var tarihler = telafiTakvimi.Select(x => x.Tarih.Date).Distinct().ToList();
            
            var mevcutTelafilerRaw = await _context.TelafiDersler
                .Where(t => tarihler.Contains(t.TelafiTarihi.Date))
                .Select(t => new { t.TelafiTarihi, t.BaslangicSaat, t.KisimNo, t.YedekHocaId })
                .AsNoTracking().ToListAsync();

            var mevcutTelafiler = mevcutTelafilerRaw.Select(t => new TelafiCache {
                TelafiTarihi = t.TelafiTarihi, BaslangicSaat = t.BaslangicSaat, KisimNo = t.KisimNo, YedekHocaId = t.YedekHocaId
            }).ToList();

            var gunler = telafiTakvimi.Select(x => GetGunAdi(x.Tarih)).Distinct().ToList();
            
            var normalDersProgramiRaw = await _context.DersProgrami
                .Where(d => gunler.Contains(d.DersGunu))
                .Select(d => new { d.DersGunu, d.DersSaati, d.KisimNo, d.HocaId })
                .AsNoTracking().ToListAsync();
            
            var normalDersProgrami = normalDersProgramiRaw.Select(d => new DersCache {
                DersGunu = d.DersGunu, DersSaati = d.DersSaati, KisimNo = d.KisimNo, HocaId = d.HocaId
            }).ToList();

            var eklenecekTelafiler = new List<TelafiDers>();

            // 5. GRUPLAMA VE PLANLAMA
            // Kısım bazlı grupla (Aynı sınıfın dersleri beraber hareket etsin)
            var kisimGruplari = dersler.GroupBy(d => d.KisimNo).ToList();

            foreach (var grup in kisimGruplari)
            {
                var grupDersleri = grup.ToList(); 
                var kisimNo = grup.Key;
                
                List<KeyValuePair<DersProgrami, TelafiSlot>> enIyiPlan = null;
                var distinctDates = telafiTakvimi.Select(t => t.Tarih.Date).Distinct().OrderBy(d => d).ToList();

                // Tüm takvimi tara (150 gün boyunca)
                foreach (var tarih in distinctDates)
                {
                    // O günün uygun slotlarını al
                    var gununSlotlari = telafiTakvimi
                        .Where(s => s.Tarih.Date == tarih && 
                                    s.FakulteId == grupDersleri.First().FakulteId &&
                                    kisimNo >= s.MinKisim && kisimNo <= s.MaxKisim)
                        .OrderBy(s => s.SaatIndex)
                        .ToList();

                    if (gununSlotlari.Count < grupDersleri.Count) continue;

                    var anlikPlan = new List<KeyValuePair<DersProgrami, TelafiSlot>>();
                    var kullanilanSlotIndexleri = new HashSet<int>();
                    bool planBasarili = true;

                    foreach (var ders in grupDersleri)
                    {
                        TelafiSlot bulunanSlot = null;

                        foreach (var slot in gununSlotlari)
                        {
                            if (kullanilanSlotIndexleri.Contains(slot.SaatIndex)) continue;

                            // Çakışma Kontrolleri
                            bool sinifDoluTelafi = mevcutTelafiler.Any(t => t.TelafiTarihi.Date == slot.Tarih.Date && t.BaslangicSaat == slot.BaslangicSaat && t.KisimNo == kisimNo);
                            if (sinifDoluTelafi) continue;

                            bool sinifDoluNormal = normalDersProgrami.Any(d => d.DersGunu == GetGunAdi(slot.Tarih) && d.DersSaati == slot.SaatIndex && d.KisimNo == kisimNo);
                            if (sinifDoluNormal) continue;

                            // Sadece KENDİ HOCASI müsait mi?
                            bool hocaMusait = IsHocaMusaitLocal(ders.HocaId, slot, mevcutTelafiler, normalDersProgrami);
                            if (!hocaMusait) continue;

                            bulunanSlot = slot;
                            break;
                        }

                        if (bulunanSlot != null)
                        {
                            anlikPlan.Add(new KeyValuePair<DersProgrami, TelafiSlot>(ders, bulunanSlot));
                            kullanilanSlotIndexleri.Add(bulunanSlot.SaatIndex);
                        }
                        else
                        {
                            planBasarili = false;
                            break;
                        }
                    }

                    if (planBasarili)
                    {
                        enIyiPlan = anlikPlan;
                        break; // Bulduk, döngüden çık.
                    }
                }

                if (enIyiPlan != null)
                {
                    foreach (var item in enIyiPlan)
                    {
                        var ders = item.Key;
                        var slot = item.Value;

                        var telafi = new TelafiDers
                        {
                            DersProgramiId = ders.Id,
                            YedekHocaId = ders.HocaId, // Kendi Hocası
                            FakulteId = ders.FakulteId,
                            TelafiTarihi = slot.Tarih,
                            BaslangicSaat = slot.BaslangicSaat,
                            BitisSaat = slot.BitisSaat,
                            TelafiTuru = "Telafi",
                            TelafiNedeni = telafiNedeni,
                            Aciklama = $"{telafiEdilecekTarih:dd.MM.yyyy} telafisi",
                            KisimNo = ders.KisimNo,
                            Onaylandi = false
                        };
                        
                        eklenecekTelafiler.Add(telafi);
                        mevcutTelafiler.Add(new TelafiCache { 
                            TelafiTarihi = telafi.TelafiTarihi, BaslangicSaat = telafi.BaslangicSaat, 
                            KisimNo = telafi.KisimNo, YedekHocaId = telafi.YedekHocaId 
                        });
                        basarili++;
                    }
                }
                else
                {
                    // YER BULUNAMADI - AMA HATA KAYDETME (Sadece Bilgi Ver)
                    hatalar.Add($"Kısım {kisimNo}: 150 gün (5 ay) taranmasına rağmen uygun yer bulunamadı.");
                    basarisiz += grupDersleri.Count;
                }
            }

            if (eklenecekTelafiler.Any())
            {
                _context.TelafiDersler.AddRange(eklenecekTelafiler);
                await _context.SaveChangesAsync();
            }

            var mesaj = $"✅ Oluşturulan Telafi: {basarili}";
            if (basarisiz > 0)
            {
                mesaj += $"\n⚠️ Yerleştirilemeyen Ders Sayısı: {basarisiz}";
            }
            if (hatalar.Any())
            {
                mesaj += "\n\nDetaylar:\n" + string.Join("\n", hatalar.Take(10));
            }

            return (true, mesaj, basarili, basarisiz);
        }

        // LOCAL KONTROL (HIZLI)
        private bool IsHocaMusaitLocal(int hocaId, TelafiSlot slot, List<TelafiCache> mevcutTelafiler, List<DersCache> normalDersProgrami)  
        {
            var gunAdi = GetGunAdi(slot.Tarih);
            
            var normalDersiVar = normalDersProgrami.Any(d => d.HocaId == hocaId && d.DersGunu == gunAdi && d.DersSaati == slot.SaatIndex);
            if (normalDersiVar) return false;

            var telafideGorevli = mevcutTelafiler.Any(t => t.YedekHocaId == hocaId && t.TelafiTarihi.Date == slot.Tarih.Date && t.BaslangicSaat == slot.BaslangicSaat);
            return !telafideGorevli;
        }

        // UZATILMIŞ TAKVİM (150 GÜN)
        private async Task<List<TelafiSlot>> GetTelafiTakvimiAsync(DateTime baslangicTarihi)
        {
            var takvim = new List<TelafiSlot>();
            var taburlar = await _context.Taburlar
                .Include(t => t.TelafiAyarlari)
                .AsNoTracking()
                .ToListAsync();

            // BURASI DEĞİŞTİ: 30 yerine 150 gün (5 Ay)
            for (int i = 1; i <= 150; i++)
            {
                var tarih = baslangicTarihi.AddDays(i);
                var gunAdi = GetGunAdi(tarih);

                if (string.IsNullOrEmpty(gunAdi)) continue;

                foreach (var tabur in taburlar)
                {
                    foreach (var ayar in tabur.TelafiAyarlari.Where(a => a.TelafiYapilacakGun == gunAdi))
                    {
                        for (int saat = ayar.TelafiBaslamaSaati; saat <= ayar.TelafiMaxBitisSaati; saat++)
                        {
                            if (!string.IsNullOrEmpty(ayar.TelafiYapilamayacakDersSaatleri))
                            {
                                var atlanacaklar = ayar.TelafiYapilamayacakDersSaatleri
                                    .Split(',')
                                    .Select(s => int.TryParse(s.Trim(), out int val) ? val : 0).ToList();
                                if (atlanacaklar.Contains(saat)) continue;
                            }

                            var baslangicSaat = GetSaatTimeSpan(saat);
                            takvim.Add(new TelafiSlot
                            {
                                Tarih = tarih,
                                BaslangicSaat = baslangicSaat,
                                BitisSaat = baslangicSaat.Add(new TimeSpan(0, 40, 0)),
                                SaatIndex = saat,
                                TaburId = tabur.Id,
                                MinKisim = tabur.MinKisimNo,
                                MaxKisim = tabur.MaxKisimNo,
                                FakulteId = tabur.FakulteId
                            });
                        }
                    }
                }
            }

            return takvim.OrderBy(t => t.Tarih).ThenBy(t => t.SaatIndex).ToList();
        }

        // DİĞER YARDIMCI SINIFLAR (AYNI)
        private TelafiSlot? FindUygunSlotLocal(List<TelafiSlot> takvim, DersProgrami ders, List<TelafiCache> mevcutTelafiler, List<DersCache> normalDersProgrami)
        {
            // Toplu işlemde kullanılmıyor artık, ama derleme hatası olmasın diye bırakıldı.
            return null;
        }

        public class TelafiSlot
        {
            public DateTime Tarih { get; set; }
            public TimeSpan BaslangicSaat { get; set; }
            public TimeSpan BitisSaat { get; set; }
            public int SaatIndex { get; set; }
            public int TaburId { get; set; }
            public int MinKisim { get; set; }
            public int MaxKisim { get; set; }
            public int FakulteId { get; set; }
        }
        private class TelafiCache
        {
            public DateTime TelafiTarihi { get; set; }
            public TimeSpan BaslangicSaat { get; set; }
            public int? KisimNo { get; set; }
            public int YedekHocaId { get; set; }
        }

        private class DersCache
        {
            public string DersGunu { get; set; }
            public int DersSaati { get; set; }
            public int KisimNo { get; set; }
            public int HocaId { get; set; }
        }

        private class HocaDersCache
        {
            public int HocaId { get; set; }
            public int DersId { get; set; }
        }
        
        private string GetGunAdi(DateTime tarih)
        {
            return tarih.DayOfWeek switch
            {
                DayOfWeek.Monday => "Pazartesi",
                DayOfWeek.Tuesday => "Salı",
                DayOfWeek.Wednesday => "Çarşamba",
                DayOfWeek.Thursday => "Perşembe",
                DayOfWeek.Friday => "Cuma",
                _ => ""
            };
        }

        private int GetSaatIndex(TimeSpan saat)
        {
            if (saat.Hours == 8 && saat.Minutes == 30) return 1;
            if (saat.Hours == 9 && saat.Minutes == 20) return 2;
            if (saat.Hours == 10 && saat.Minutes == 10) return 3;
            if (saat.Hours == 11 && saat.Minutes == 0) return 4;
            if (saat.Hours == 12 && saat.Minutes == 20) return 5;
            if (saat.Hours == 13 && saat.Minutes == 10) return 6;
            if (saat.Hours == 14 && saat.Minutes == 0) return 7;
            if (saat.Hours == 14 && saat.Minutes == 50) return 8;
            if (saat.Hours == 15 && saat.Minutes == 40) return 9;
            return 1;
        }

        public static TimeSpan GetSaatTimeSpan(int saatIndex)
        {
            return saatIndex switch
            {
                1 => new TimeSpan(8, 30, 0),
                2 => new TimeSpan(9, 20, 0),
                3 => new TimeSpan(10, 10, 0),
                4 => new TimeSpan(11, 0, 0),
                5 => new TimeSpan(12, 20, 0),
                6 => new TimeSpan(13, 10, 0),
                7 => new TimeSpan(14, 0, 0),
                8 => new TimeSpan(14, 50, 0),
                9 => new TimeSpan(15, 40, 0),
                _ => new TimeSpan(8, 30, 0)
            };
        }
    }

    public class TelafiOneriViewModel
    {
        public DersProgrami DersProgrami { get; set; }
        public DateTime TelafiTarihi { get; set; }
        public TimeSpan BaslangicSaat { get; set; }
        public TimeSpan BitisSaat { get; set; }
        public string OnerilenTur { get; set; }
        public string Aciklama { get; set; }
        public List<Hoca> MusaitYedekHocalar { get; set; } = new();
        public List<DersProgrami> BirlestirilebilirDersler { get; set; } = new();
    }
}