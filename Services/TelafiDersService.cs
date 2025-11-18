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

        // Telafi önerisi al - GELİŞTİRİLMİŞ
        public async Task<TelafiOneriViewModel> GetTelafiOneriAsync(int dersProgramiId, DateTime telafiTarihi, TimeSpan baslangicSaat, TimeSpan bitisSaat)
        {
            var ders = await _context.DersProgrami
                .Include(d => d.Hoca)
                .Include(d => d.Fakulte)
                .Include(d => d.Ders)
                .FirstOrDefaultAsync(d => d.Id == dersProgramiId);

            if (ders == null)
                return null;

            var oneri = new TelafiOneriViewModel
            {
                DersProgrami = ders,
                TelafiTarihi = telafiTarihi,
                BaslangicSaat = baslangicSaat,
                BitisSaat = bitisSaat
            };

            // 1. TELAFİ kontrolü: Aynı hoca müsait mi?
            bool hocaMusait = await IsHocaMusaitAsync(ders.HocaId, telafiTarihi, baslangicSaat, bitisSaat);

            if (hocaMusait)
            {
                oneri.OnerilenTur = "Telafi";
                oneri.Aciklama = $"Telafi dersi yapılabilir. Aynı hoca ({ders.Hoca.AdSoyad}) dersi verebilir.";
                oneri.MusaitYedekHocalar = new List<Hoca> { ders.Hoca };
                return oneri;
            }

            // 2. İKAME kontrolü: Başka hocalar müsait mi?
            var musaitHocalar = await GetMusaitHocalarAsync(ders.FakulteId, ders.HocaId, telafiTarihi, baslangicSaat, bitisSaat, ders.DersId);

            if (musaitHocalar.Any())
            {
                oneri.OnerilenTur = "İkame";
                oneri.Aciklama = $"İkame dersi önerilir. {musaitHocalar.Count} müsait hoca bulundu.";
                oneri.MusaitYedekHocalar = musaitHocalar;
                return oneri;
            }

            // 3. BİRLEŞTİRME: Hiç müsait hoca yok
            oneri.OnerilenTur = "Birleştirme";
            oneri.Aciklama = "Uygun hoca bulunamadı. Dersin başka bir bölümle birleştirilmesi önerilir.";
            oneri.MusaitYedekHocalar = new List<Hoca>();

            // Birleştirilebilecek dersler
            oneri.BirlestirilebilirDersler = await GetBirlestirilebilirDerslerAsync(dersProgramiId, telafiTarihi);

            return oneri;
        }

        // Hocanın müsait olup olmadığını kontrol et
        private async Task<bool> IsHocaMusaitAsync(int hocaId, DateTime tarih, TimeSpan baslangic, TimeSpan bitis)
        {
            // Normal ders programında mı?
            var normalDersiVar = await _context.DersProgrami.AnyAsync(d =>
                d.HocaId == hocaId &&
                d.DersGunu == GetGunAdi(tarih) &&
                d.DersSaati == GetSaatIndex(baslangic));

            if (normalDersiVar)
                return false;

            // Başka telafi dersinde mi?
            var telafideGorevli = await _context.TelafiDersler.AnyAsync(t =>
                t.YedekHocaId == hocaId &&
                t.TelafiTarihi.Date == tarih.Date &&
                ((t.BaslangicSaat < bitis && t.BitisSaat > baslangic)));

            return !telafideGorevli;
        }

        // Müsait hocaları getir
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

                    if (!dersVerebilir)
                        continue;
                }

                bool musait = await IsHocaMusaitAsync(hoca.Id, tarih, baslangic, bitis);

                if (musait)
                {
                    musaitHocalar.Add(hoca);
                }
            }

            return musaitHocalar.OrderBy(h => h.AdSoyad).ToList();
        }

        // Birleştirilebilir dersleri bul
        private async Task<List<DersProgrami>> GetBirlestirilebilirDerslerAsync(int dersProgramiId, DateTime telafiTarihi)
        {
            var anaDers = await _context.DersProgrami
                .Include(d => d.Hoca)
                .Include(d => d.Fakulte)
                .FirstOrDefaultAsync(d => d.Id == dersProgramiId);

            if (anaDers == null)
                return new List<DersProgrami>();

            var gunAdi = GetGunAdi(telafiTarihi);

            var birlestirilebilirler = await _context.DersProgrami
                .Include(d => d.Hoca)
                .Where(d =>
                    d.Id != dersProgramiId &&
                    d.FakulteId == anaDers.FakulteId &&
                    d.DersGunu == gunAdi &&
                    d.DersId == anaDers.DersId)
                .ToListAsync();

            return birlestirilebilirler;
        }

        // Telafi dersi oluştur
        public async Task<(bool Success, string Message)> CreateTelafiDersAsync(TelafiDers telafiDers)
        {
            var hocaMesgul = await _context.TelafiDersler.AnyAsync(t =>
                t.YedekHocaId == telafiDers.YedekHocaId &&
                t.TelafiTarihi.Date == telafiDers.TelafiTarihi.Date &&
                ((t.BaslangicSaat < telafiDers.BitisSaat && t.BitisSaat > telafiDers.BaslangicSaat)));

            if (hocaMesgul)
                return (false, "Seçilen hoca bu saatte başka bir telafi dersinde görevli.");

            var normalDersi = await _context.DersProgrami.AnyAsync(d =>
                d.HocaId == telafiDers.YedekHocaId &&
                d.DersGunu == GetGunAdi(telafiDers.TelafiTarihi) &&
                d.DersSaati == GetSaatIndex(telafiDers.BaslangicSaat));

            if (normalDersi)
                return (false, "Seçilen hocanın bu saatte normal dersi var.");

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

            if (telafi == null)
                return (false, "Telafi dersi bulunamadı.");

            _context.TelafiDersler.Remove(telafi);
            await _context.SaveChangesAsync();

            return (true, "Telafi dersi silindi.");
        }

        public async Task<(bool Success, string Message)> UpdateTelafiDersAsync(TelafiDers telafiDers)
        {
            var mevcutTelafi = await _context.TelafiDersler.FindAsync(telafiDers.Id);
            if (mevcutTelafi == null)
                return (false, "Telafi dersi bulunamadı.");

            var hocaMesgul = await _context.TelafiDersler.AnyAsync(t =>
                t.Id != telafiDers.Id &&
                t.YedekHocaId == telafiDers.YedekHocaId &&
                t.TelafiTarihi.Date == telafiDers.TelafiTarihi.Date &&
                ((t.BaslangicSaat < telafiDers.BitisSaat && t.BitisSaat > telafiDers.BaslangicSaat)));

            if (hocaMesgul)
                return (false, "Seçilen hoca bu saatte başka bir telafi dersinde görevli.");

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

            if (telafi == null)
                return (false, "Telafi dersi bulunamadı.");

            telafi.Onaylandi = true;
            await _context.SaveChangesAsync();

            return (true, "Telafi dersi onaylandı.");
        }

        // 🚀 OPTİMİZE EDİLMİŞ: AKILLI TOPLU TELAFİ - BATCH PROCESSING + LOCAL CACHE
        public async Task<(bool Success, string Message, int Basarili, int Basarisiz)> TopluTelafiOlusturAsync(
            DateTime telafiEdilecekTarih,
            string telafiNedeni)
        {
            int basarili = 0;
            int basarisiz = 0;
            var hatalar = new List<string>();

            // 1. Seçilen tarihin gününü al
            var gun = GetGunAdi(telafiEdilecekTarih);
            
            if (string.IsNullOrEmpty(gun))
                return (false, "Seçilen tarih hafta içi bir gün olmalıdır (Pzt-Cum).", 0, 0);

            // 2. ✅ OPTİMİZASYON: AsNoTracking ile yükle (3x daha hızlı)
            var dersler = await _context.DersProgrami
                .Include(d => d.Hoca)
                .Include(d => d.Fakulte)
                .Include(d => d.Ders)
                .Where(d => d.DersGunu == gun)
                .OrderBy(d => d.FakulteId)
                .ThenBy(d => d.KisimNo)
                .ThenBy(d => d.DersSaati)
                .AsNoTracking() // ✅ Read-only
                .ToListAsync();

            if (!dersler.Any())
                return (false, $"{telafiEdilecekTarih:dd.MM.yyyy} ({gun}) günü için ders bulunamadı.", 0, 0);

            // 3. Telafi takvimi oluştur
            var telafiTakvimi = await GetTelafiTakvimiAsync(telafiEdilecekTarih);
            
            if (!telafiTakvimi.Any())
                return (false, "Telafi için uygun gün/saat ayarı bulunamadı. Lütfen Tabur Telafi Ayarlarını kontrol edin.", 0, 0);

            // ✅ OPTİMİZASYON: Tüm verileri ÖN CACHE'e al (binlerce DB query yerine 3 query)
            var tarihler = telafiTakvimi.Select(x => x.Tarih.Date).Distinct().ToList();

            var mevcutTelafilerRaw = await _context.TelafiDersler
                .Where(t => tarihler.Contains(t.TelafiTarihi.Date))
                .Select(t => new { t.TelafiTarihi, t.BaslangicSaat, t.KisimNo, t.YedekHocaId })
                .AsNoTracking()
                .ToListAsync();

            var mevcutTelafiler = mevcutTelafilerRaw.Select(t => new TelafiCache
            {
                TelafiTarihi = t.TelafiTarihi,
                BaslangicSaat = t.BaslangicSaat,
                KisimNo = t.KisimNo,
                YedekHocaId = t.YedekHocaId
            }).ToList();

            var gunler = telafiTakvimi.Select(x => GetGunAdi(x.Tarih)).Distinct().ToList();

            var normalDersProgramiRaw = await _context.DersProgrami
            .Where(d => gunler.Contains(d.DersGunu))
            .Select(d => new { d.DersGunu, d.DersSaati, d.KisimNo, d.HocaId })
            .AsNoTracking()
            .ToListAsync();
            var normalDersProgrami = normalDersProgramiRaw.Select(d => new DersCache
            {
                DersGunu = d.DersGunu,
                DersSaati = d.DersSaati,
                KisimNo = d.KisimNo,
                HocaId = d.HocaId
            }).ToList();

            var hocaDersYetkileriRaw = await _context.HocaDersler
                .Select(hd => new { hd.HocaId, hd.DersId })
                .AsNoTracking()
                .ToListAsync();

            var hocaDersYetkileri = hocaDersYetkileriRaw.Select(hd => new HocaDersCache
            {
                HocaId = hd.HocaId,
                DersId = hd.DersId
            }).ToList();

            var tumHocalar = await _context.Hocalar
                .Where(h => h.AktifMi)
                .AsNoTracking()
                .ToListAsync();

            // ✅ OPTİMİZASYON: Batch processing (her 50 kayıtta bir kaydet)
            var eklenecekTelafiler = new List<TelafiDers>();
            const int batchSize = 50;

            // 4. Her ders için telafi ataması yap (LOCAL DATA kullanarak)
            foreach (var ders in dersler)
            {
                try
                {
                    var uygunSlot = FindUygunSlotLocal(
                        telafiTakvimi, 
                        ders, 
                        mevcutTelafiler, 
                        normalDersProgrami);

                    if (uygunSlot == null)
                    {
                        hatalar.Add($"{ders.DersAdi} (Kısım {ders.KisimNo}): Uygun slot bulunamadı");
                        basarisiz++;
                        continue;
                    }

                    // Hoca müsaitlik kontrolü (local data)
                    bool hocaMusait = IsHocaMusaitLocal(
                        ders.HocaId, 
                        uygunSlot, 
                        mevcutTelafiler, 
                        normalDersProgrami);

                    int yedekHocaId;
                    string telafiTuru;

                    if (hocaMusait)
                    {
                        yedekHocaId = ders.HocaId;
                        telafiTuru = "Telafi";
                    }
                    else
                    {
                        // Müsait hoca bul (local data)
                        var musaitHocalar = FindMusaitHocalarLocal(
                            ders,
                            uygunSlot,
                            tumHocalar,
                            hocaDersYetkileri,
                            mevcutTelafiler,
                            normalDersProgrami);

                        if (!musaitHocalar.Any())
                        {
                            hatalar.Add($"{ders.DersAdi} (Kısım {ders.KisimNo}): Müsait hoca bulunamadı");
                            basarisiz++;
                            continue;
                        }

                        yedekHocaId = musaitHocalar.First().Id;
                        telafiTuru = "İkame";
                    }

                    var telafi = new TelafiDers
                    {
                        DersProgramiId = ders.Id,
                        YedekHocaId = yedekHocaId,
                        FakulteId = ders.FakulteId,
                        TelafiTarihi = uygunSlot.Tarih,
                        BaslangicSaat = uygunSlot.BaslangicSaat,
                        BitisSaat = uygunSlot.BitisSaat,
                        TelafiTuru = telafiTuru,
                        TelafiNedeni = telafiNedeni,
                        Aciklama = $"{telafiEdilecekTarih:dd.MM.yyyy} {gun} günü yerine",
                        KisimNo = ders.KisimNo,
                        Onaylandi = false,
                        CiktiAlindi = false
                    };

                    eklenecekTelafiler.Add(telafi);

                    // Local cache'e ekle
                    mevcutTelafiler.Add(new TelafiCache
                    {
                        TelafiTarihi = telafi.TelafiTarihi,
                        BaslangicSaat = telafi.BaslangicSaat,
                        KisimNo = telafi.KisimNo,
                        YedekHocaId = telafi.YedekHocaId
                    });

                    basarili++;

                    // ✅ Her 50 kayıtta bir veritabanına yaz (Timeout önleme)
                    if (eklenecekTelafiler.Count >= batchSize)
                    {
                        _context.TelafiDersler.AddRange(eklenecekTelafiler);
                        await _context.SaveChangesAsync();
                        eklenecekTelafiler.Clear();
                    }
                }
                catch (Exception ex)
                {
                    hatalar.Add($"{ders.DersAdi} (Kısım {ders.KisimNo}): {ex.Message}");
                    basarisiz++;
                }
            }

            // ✅ Kalan kayıtları kaydet
            if (eklenecekTelafiler.Any())
            {
                _context.TelafiDersler.AddRange(eklenecekTelafiler);
                await _context.SaveChangesAsync();
            }

            var mesaj = $"✅ Başarılı: {basarili}, ❌ Başarısız: {basarisiz}";
            if (hatalar.Any())
            {
                // İlk 10 hatayı göster
                var gosterilecekHatalar = hatalar.Take(10).ToList();
                mesaj += "\n\n⚠️ Hatalar (İlk 10):\n" + string.Join("\n", gosterilecekHatalar);
                
                if (hatalar.Count > 10)
                    mesaj += $"\n\n... ve {hatalar.Count - 10} hata daha.";
            }

            return (true, mesaj, basarili, basarisiz);
        }

        // ✅ LOCAL DATA ile slot bulma (DB query yok)
        private TelafiSlot? FindUygunSlotLocal(
            List<TelafiSlot> takvim,
            DersProgrami ders,
            List<TelafiCache> mevcutTelafiler,
            List<DersCache> normalDersProgrami)
        {
            foreach (var slot in takvim.Where(s => s.FakulteId == ders.FakulteId))
            {
                // Kısım aralığı kontrolü
                if (ders.KisimNo < slot.MinKisim || ders.KisimNo > slot.MaxKisim)
                    continue;

                // Mevcut telafi kontrolü (local)
                var mevcutTelafi = mevcutTelafiler.Any(t =>
                    t.TelafiTarihi.Date == slot.Tarih.Date &&
                    t.BaslangicSaat == slot.BaslangicSaat &&
                    t.KisimNo == ders.KisimNo);

                if (mevcutTelafi)
                    continue;

                // Normal ders kontrolü (local)
                var gunAdi = GetGunAdi(slot.Tarih);
                var normalDers = normalDersProgrami.Any(d =>
                    d.DersGunu == gunAdi &&
                    d.DersSaati == slot.SaatIndex &&
                    d.KisimNo == ders.KisimNo);

                if (normalDers)
                    continue;

                return slot;
            }

            return null;
        }

        // ✅ LOCAL DATA ile hoca müsaitlik kontrolü
        private bool IsHocaMusaitLocal(
            int hocaId,
            TelafiSlot slot,
            List<TelafiCache> mevcutTelafiler, 
            List<DersCache> normalDersProgrami)  
        {
            var gunAdi = GetGunAdi(slot.Tarih);

            // Normal ders kontrolü
            var normalDersiVar = normalDersProgrami.Any(d =>
                d.HocaId == hocaId &&
                d.DersGunu == gunAdi &&
                d.DersSaati == slot.SaatIndex);

            if (normalDersiVar)
                return false;

            // Telafi dersi kontrolü
            var telafideGorevli = mevcutTelafiler.Any(t =>
                t.YedekHocaId == hocaId &&
                t.TelafiTarihi.Date == slot.Tarih.Date &&
                t.BaslangicSaat == slot.BaslangicSaat);

            return !telafideGorevli;
        }

        // ✅ LOCAL DATA ile müsait hoca bulma
        private List<Hoca> FindMusaitHocalarLocal(
            DersProgrami ders,
            TelafiSlot slot,
            List<Hoca> tumHocalar,
            List<HocaDersCache> hocaDersYetkileri,  
            List<TelafiCache> mevcutTelafiler,  
            List<DersCache> normalDersProgrami)  
        {
            var musaitHocalar = new List<Hoca>();

            foreach (var hoca in tumHocalar.Where(h => 
                h.FakulteId == ders.FakulteId && 
                h.Id != ders.HocaId))
            {
                // Ders yetkisi kontrolü
                if (ders.DersId.HasValue)
                {
                    bool dersVerebilir = hocaDersYetkileri.Any(hd =>
                        hd.HocaId == hoca.Id && hd.DersId == ders.DersId.Value);

                    if (!dersVerebilir)
                        continue;
                }

                // Müsaitlik kontrolü
                bool musait = IsHocaMusaitLocal(
                    hoca.Id, 
                    slot, 
                    mevcutTelafiler, 
                    normalDersProgrami);

                if (musait)
                    musaitHocalar.Add(hoca);
            }

            return musaitHocalar.OrderBy(h => h.AdSoyad).ToList();
        }

        private async Task<List<TelafiSlot>> GetTelafiTakvimiAsync(DateTime baslangicTarihi)
        {
            var takvim = new List<TelafiSlot>();
            var taburlar = await _context.Taburlar
                .Include(t => t.TelafiAyarlari)
                .AsNoTracking()
                .ToListAsync();

            for (int i = 1; i <= 30; i++)
            {
                var tarih = baslangicTarihi.AddDays(i);
                var gunAdi = GetGunAdi(tarih);

                if (string.IsNullOrEmpty(gunAdi))
                    continue;

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
                                    .Select(s => int.TryParse(s.Trim(), out int val) ? val : 0)
                                    .Where(v => v > 0)
                                    .ToList();

                                if (atlanacaklar.Contains(saat))
                                    continue;
                            }

                            var baslangicSaat = GetSaatTimeSpan(saat);
                            var bitisSaat = baslangicSaat.Add(new TimeSpan(0, 40, 0));

                            takvim.Add(new TelafiSlot
                            {
                                Tarih = tarih,
                                BaslangicSaat = baslangicSaat,
                                BitisSaat = bitisSaat,
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
        // Yardımcı metodlar
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

    // ViewModel
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
