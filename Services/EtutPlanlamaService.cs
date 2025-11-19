using Etutlist.Models;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Services
{
    public class EtutPlanlamaService
    {
        private readonly AppDbContext _db;
        public EtutPlanlamaService(AppDbContext db) { _db = db; }

        public async Task GenerateMonthlyPlan(DateTime startDate)
        {
            var endDate = startDate.AddMonths(1);

            bool planExists = await _db.Etutler.AnyAsync(e => e.Tarih >= startDate && e.Tarih < endDate);
            
            if (planExists)
            {
                await DeleteMonthlyPlan(startDate);
            }

            // ? SOFT DELETE: Sadece aktif personeller
            var people = await _db.Personeller
                .Where(p => p.AktifMi)
                .Include(p => p.Mazeretler)
                .ToListAsync();

            // ✅ Özel günleri çek (bir sonraki ayın ilk günü dahil - önceki gün kontrolü için)
            var ozelGunSet = await _db.OzelGunler
                .Where(x => x.Tarih >= startDate && x.Tarih <= endDate) // <= endDate (1 gün sonrasını dahil et)
                .Select(x => x.Tarih.Date)
                .ToHashSetAsync();

            // ? ÖZEL GRUPLARI YÜK
            var ozelGruplar = await _db.OzelGruplar
                .Where(g => g.AktifMi)
                .Include(g => g.Uyeler)
                    .ThenInclude(u => u.Personel)
                        .ThenInclude(p => p.Mazeretler)
                .ToListAsync();

            var rnd = new Random();

            // ✅ Sadece özel günden 1 gün öncesini çıkar (özel günün kendisinde etüt VAR)
            var ozelGundenOncekiGunler = new HashSet<DateTime>();
            foreach (var ozelGun in ozelGunSet)
            {
                ozelGundenOncekiGunler.Add(ozelGun.AddDays(-1)); // Sadece özel günden 1 gün önce
            }

            // ✅ Tüm etüt günlerini al (Sadece özel günden 1 gün öncesinde etüt yok)
            var allDays = Enumerable.Range(0, (endDate - startDate).Days)
                .Select(i => startDate.AddDays(i))
                .Where(d => IsEtutGunu(d) && !ozelGundenOncekiGunler.Contains(d.Date))
                .ToList();

            var pazarlar = allDays.Where(d => d.DayOfWeek == DayOfWeek.Sunday).ToList();
            var ozelGunler = allDays.Where(d => ozelGunSet.Contains(d.Date)).ToList();
            var haftaIci = allDays.Except(pazarlar).Except(ozelGunler).ToList();

            // ✅ GRUPLARI ADİL DAĞITIM İÇİN SIRALA VE ANALİZ ET
            var gruplarSirali = ozelGruplar
                .Select(g => new
                {
                    Grup = g,
                    Uyeler = g.Uyeler.Select(u => u.Personel).ToList(),
                    OrtalamaPazar = g.Uyeler.Average(u => (double)u.Personel.PazarSayisi),
                    OrtalamaHaftaIci = g.Uyeler.Average(u => (double)u.Personel.HaftaIciSayisi),
                    OrtalamaOzelGun = g.Uyeler.Average(u => (double)u.Personel.OzelGunSayisi)
                })
                .OrderBy(x => x.OrtalamaPazar + x.OrtalamaHaftaIci + x.OrtalamaOzelGun) // Toplam az tutanlar önce
                .ToList();

            // ✅ Her grup için hangi gün türünde atanması gerektiğini belirle
            var grupGunTuruOncelikleri = new Dictionary<int, string>(); // GrupId -> "Pazar"|"OzelGun"|"HaftaIci"

            foreach (var grupInfo in gruplarSirali)
            {
                var ortalamalar = new[]
                {
                    ("Pazar", grupInfo.OrtalamaPazar),
                    ("OzelGun", grupInfo.OrtalamaOzelGun),
                    ("HaftaIci", grupInfo.OrtalamaHaftaIci)
                }.OrderBy(x => x.Item2).ToList(); // En az tuttuğu türü bul
                
                grupGunTuruOncelikleri[grupInfo.Grup.Id] = ortalamalar[0].Item1;
            }

            // ? Atanmış grupları takip et
            var atananGruplar = new HashSet<int>();

            foreach (var d in allDays)
            {
                var adaylar = people.Where(p => !HasMazeret(p, d)).ToList();
                bool gunTipiPazar = d.DayOfWeek == DayOfWeek.Sunday;
                bool gunTipiOzel = ozelGunSet.Contains(d.Date);

                // ID 20 olan personeli Pazar günü adaylardan çıkar
                if (gunTipiPazar)
                {
                    adaylar = adaylar.Where(p => p.Id != 20).ToList();
                }

                List<Personel> secilecek = new List<Personel>();

                // ✅ 1. ATANMAMIŞ GRUP VAR MI VE BU GÜN O GRUP İÇİN UYGUN MU?
                OzelGrup? atanacakGrup = null;

                foreach (var grupInfo in gruplarSirali.Where(g => !atananGruplar.Contains(g.Grup.Id)))
                {
                    var grup = grupInfo.Grup;
                    var uyeler = grupInfo.Uyeler;
                    
                    // Bu grubun öncelikli gün türünü al
                    string oncelikliTur = grupGunTuruOncelikleri[grup.Id];
                    
                    // Bu gün o türden mi?
                    bool gunUygun = false;
                    if (oncelikliTur == "Pazar" && gunTipiPazar) gunUygun = true;
                    else if (oncelikliTur == "OzelGun" && gunTipiOzel) gunUygun = true;
                    else if (oncelikliTur == "HaftaIci" && !gunTipiPazar && !gunTipiOzel) gunUygun = true;
                    
                    // Eğer bu gün uygun değilse ama ayın sonuna yaklaşıldıysa yine de ata
                    int kalanGunSayisi = allDays.Count(day => day >= d);
                    int atanmamisGrupSayisi = ozelGruplar.Count - atananGruplar.Count;
                    
                    if (!gunUygun && kalanGunSayisi > atanmamisGrupSayisi * 2) 
                        continue; // Hala zaman var, uygun günü bekle
                    
                    // Tüm üyeler müsait mi?
                    bool tumUyelerMusait = uyeler.All(u => adaylar.Any(a => a.Id == u.Id));

                    if (tumUyelerMusait && uyeler.Count >= 2 && uyeler.Count <= 4)
                    {
                        atanacakGrup = grup;
                        break;
                    }
                }

                // ✅ 2. GRUP ATANACAKSA
                if (atanacakGrup != null)
                {
                    var grupUyeleri = atanacakGrup.Uyeler.Select(u => u.Personel).ToList();
                    secilecek.AddRange(grupUyeleri);
                    
                    // Grup üyelerini adaylardan çıkar
                    foreach (var uye in grupUyeleri)
                    {
                        adaylar.RemoveAll(a => a.Id == uye.Id);
                    }
                    
                    // Grup tam değilse (4 kişi değilse), tamamla
                    int eksik = 4 - secilecek.Count;
                    if (eksik > 0)
                    {
                        // Diğer grupların üyelerini çıkar
                        var digerGrupUyeIds = ozelGruplar
                            .Where(g => g.Id != atanacakGrup.Id)
                            .SelectMany(g => g.Uyeler.Select(u => u.PersonelId))
                            .ToHashSet();
                        
                        var grupDisiAdaylar = adaylar.Where(a => !digerGrupUyeIds.Contains(a.Id)).ToList();
                        secilecek.AddRange(SecimKurallari(grupDisiAdaylar, d, ozelGunSet).Take(eksik));
                    }
                    
                    atananGruplar.Add(atanacakGrup.Id);
                }
                // ✅ 3. GRUP YOK İSE, NORMAL MANTIKLA DEVAM ET
                else
                {
                    // Henüz atanmamış grup üyelerini çıkar (onlar sadece grup halinde atanmalı)
                    var atanmamisGrupUyeIds = ozelGruplar
                        .Where(g => !atananGruplar.Contains(g.Id))
                        .SelectMany(g => g.Uyeler.Select(u => u.PersonelId))
                        .ToHashSet();
                    
                    var grupDisiAdaylar = adaylar.Where(a => !atanmamisGrupUyeIds.Contains(a.Id)).ToList();

                    secilecek = SecimKurallari(grupDisiAdaylar, d, ozelGunSet).Take(4).ToList();
                }

                if (!secilecek.Any()) continue;

                KaydetVeSayacArtir(d, secilecek, ozelGunSet);
                await _db.SaveChangesAsync();
            }

            await GenerateMonthlyYedekList(startDate);
        }

        private IEnumerable<Personel> SecimKurallari(List<Personel> adaylar, DateTime d, HashSet<DateTime> ozelGunSet)
        {
            if (d.DayOfWeek == DayOfWeek.Sunday)
            {
                return adaylar
                    .OrderBy(p => p.PazarSayisi)
                    .ThenBy(p => p.HaftaIciSayisi + p.OzelGunSayisi);
            }
            else if (ozelGunSet.Contains(d.Date))
            {
                return adaylar
                    .OrderBy(p => p.OzelGunSayisi)
                    .ThenBy(p => p.HaftaIciSayisi + p.PazarSayisi);
            }
            else
            {
                return adaylar
                    .OrderBy(p => p.HaftaIciSayisi)
                    .ThenBy(p => p.PazarSayisi + p.OzelGunSayisi);
            }
        }

        private void KaydetVeSayacArtir(DateTime d, List<Personel> secilenler, HashSet<DateTime> ozelGunSet)
        {
            int sinifNo = 1;
            foreach (var hoca in secilenler)
            {
                _db.Etutler.Add(new Etut
                {
                    PersonelId = hoca.Id,
                    Tarih = d,
                    Tip = "Normal",
                    SinifNo = sinifNo++
                });

                if (d.DayOfWeek == DayOfWeek.Sunday)
                    hoca.PazarSayisi++;
                else if (ozelGunSet.Contains(d.Date))
                    hoca.OzelGunSayisi++;
                else
                    hoca.HaftaIciSayisi++;
            }
        }

        public async Task<List<Personel>> GenerateMonthlyYedekList(DateTime startDate, int yedekSayisi = 15)
        {
            int yil = startDate.Year;
            int ay = startDate.Month;
            int seed = yil * 100 + ay;

            var endDate = startDate.AddMonths(1);
            int toplamGunSayisi = (endDate - startDate).Days;
            int minMusaitGunSayisi = 10; // En az 10 gün müsait olmalı

            // ✅ Aktif personelleri mazeret bilgileri ile birlikte çek
            var people = await _db.Personeller
                .Where(p => p.AktifMi)
                .Include(p => p.Mazeretler)
                .ToListAsync();

            // ✅ Personelleri mazeret gün sayısına göre filtrele
            var uygunPersoneller = people.Where(p =>
            {
                // Bu aydaki mazeret günlerini hesapla
                int mazeretGunSayisi = 0;
                foreach (var mazeret in p.Mazeretler)
                {
                    // Mazeret bu ay içinde mi?
                    var mazeretBaslangic = mazeret.Baslangic.Date < startDate ? startDate : mazeret.Baslangic.Date;
                    var mazeretBitis = mazeret.Bitis.Date >= endDate ? endDate.AddDays(-1) : mazeret.Bitis.Date;

                    if (mazeretBitis >= mazeretBaslangic)
                    {
                        mazeretGunSayisi += (mazeretBitis - mazeretBaslangic).Days + 1;
                    }
                }

                // Müsait gün sayısı hesapla
                int musaitGunSayisi = toplamGunSayisi - mazeretGunSayisi;
                
                // En az 10 gün müsait olmalı (veya %50'den fazla müsait)
                return musaitGunSayisi >= minMusaitGunSayisi;
            }).ToList();

            var prepared = uygunPersoneller
                .Select(p => new
                {
                    Personel = p,
                    Primary = p.YedekSayisi,
                    TieBreaker = HashTie(seed, p.Id)
                })
                .OrderBy(x => x.Primary)
                .ThenBy(x => x.TieBreaker)
                .Select(x => x.Personel)
                .Take(Math.Min(yedekSayisi, uygunPersoneller.Count))
                .ToList();

            foreach (var p in prepared)
                p.YedekSayisi++;

            _db.Personeller.UpdateRange(prepared);

            int sira = 1;
            foreach (var p in prepared)
            {
                _db.AylikYedekListeleri.Add(new AylikYedekListesi
                {
                    Yil = yil,
                    Ay = ay,
                    PersonelId = p.Id,
                    Sira = sira++
                });
            }

            await _db.SaveChangesAsync();

            return prepared;
        }

        public async Task<List<Personel>> PeekMonthlyYedekList(DateTime startDate, int yedekSayisi = 15)
        {
            int yil = startDate.Year;
            int ay = startDate.Month;

            var yedekListesi = await _db.AylikYedekListeleri
                .Include(a => a.Personel)
                .Where(a => a.Yil == yil && a.Ay == ay)
                .OrderBy(a => a.Sira)
                .Select(a => a.Personel)
                .ToListAsync();

            if (yedekListesi.Any())
            {
                return yedekListesi;
            }

            var endDate = startDate.AddMonths(1);
            int toplamGunSayisi = (endDate - startDate).Days;
            int minMusaitGunSayisi = 15; // En az 10 gün müsait olmalı

            // ✅ Aktif personelleri mazeret bilgileri ile birlikte çek
            var people = await _db.Personeller
                .Where(p => p.AktifMi)
                .Include(p => p.Mazeretler)
                .AsNoTracking()
                .ToListAsync();

            // ✅ Personelleri mazeret gün sayısına göre filtrele
            var uygunPersoneller = people.Where(p =>
            {
                // Bu aydaki mazeret günlerini hesapla
                int mazeretGunSayisi = 0;
                foreach (var mazeret in p.Mazeretler)
                {
                    // Mazeret bu ay içinde mi?
                    var mazeretBaslangic = mazeret.Baslangic.Date < startDate ? startDate : mazeret.Baslangic.Date;
                    var mazeretBitis = mazeret.Bitis.Date >= endDate ? endDate.AddDays(-1) : mazeret.Bitis.Date;

                    if (mazeretBitis >= mazeretBaslangic)
                    {
                        mazeretGunSayisi += (mazeretBitis - mazeretBaslangic).Days + 1;
                    }
                }

                // Müsait gün sayısı hesapla
                int musaitGunSayisi = toplamGunSayisi - mazeretGunSayisi;
                
                // En az 10 gün müsait olmalı
                return musaitGunSayisi >= minMusaitGunSayisi;
            }).ToList();

            int seed = yil * 100 + ay;

            var prepared = uygunPersoneller
                .Select(p => new
                {
                    Personel = p,
                    Primary = p.YedekSayisi,
                    TieBreaker = HashTie(seed, p.Id)
                })
                .OrderBy(x => x.Primary)
                .ThenBy(x => x.TieBreaker)
                .Select(x => x.Personel)
                .Take(Math.Min(yedekSayisi, uygunPersoneller.Count))
                .ToList();

            return prepared;
        }

        public async Task<List<Personel>> GetOrGenerateYedekList(DateTime startDate, int yedekSayisi = 15)
        {
            int yil = startDate.Year;
            int ay = startDate.Month;

            bool alreadyExists = await _db.Etutler.AnyAsync(e => e.Tarih.Year == yil && e.Tarih.Month == ay);
            if (alreadyExists)
                return await PeekMonthlyYedekList(startDate, yedekSayisi);

            return await GenerateMonthlyYedekList(startDate, yedekSayisi);
        }

        private int HashTie(int seed, int id)
        {
            unchecked
            {
                int h = seed;
                h = h * 397 ^ id;
                return Math.Abs(h);
            }
        }

        public async Task<Dictionary<DateTime, List<Etut>>> GetPlan(DateTime startDate)
        {
            var endDate = startDate.AddMonths(1);
            return await _db.Etutler
                .Include(e => e.Personel)
                .Where(e => e.Tarih >= startDate && e.Tarih < endDate)
                .GroupBy(e => e.Tarih)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());
        }

        public async Task<List<AyBilgisi>> GetAllMonths()
        {
            var aylar = await _db.Etutler
                .Select(e => new AyBilgisi { Yil = e.Tarih.Year, Ay = e.Tarih.Month })
                .Distinct()
                .ToListAsync();

            var now = DateTime.Now;
            var buAy = new AyBilgisi { Yil = now.Year, Ay = now.Month };
            var oncekiAy = new AyBilgisi { Yil = now.AddMonths(-1).Year, Ay = now.AddMonths(-1).Month };
            var sonrakiAy = new AyBilgisi { Yil = now.AddMonths(1).Year, Ay = now.AddMonths(1).Month };

            if (!aylar.Any(x => x.Yil == buAy.Yil && x.Ay == buAy.Ay)) aylar.Add(buAy);
            if (!aylar.Any(x => x.Yil == oncekiAy.Yil && x.Ay == oncekiAy.Ay)) aylar.Add(oncekiAy);
            if (!aylar.Any(x => x.Yil == sonrakiAy.Yil && x.Ay == sonrakiAy.Ay)) aylar.Add(sonrakiAy);

            return aylar
                .OrderByDescending(x => x.Yil)
                .ThenByDescending(x => x.Ay)
                .ToList();
        }

        public async Task DeleteMonthlyPlan(DateTime startDate)
        {
            var endDate = startDate.AddMonths(1);
            int yil = startDate.Year;
            int ay = startDate.Month;

            var yedekListesiKayitlari = await _db.AylikYedekListeleri
                .Include(a => a.Personel)
                .Where(a => a.Yil == yil && a.Ay == ay)
                .ToListAsync();

            foreach (var kayit in yedekListesiKayitlari)
            {
                kayit.Personel.YedekSayisi = Math.Max(0, kayit.Personel.YedekSayisi - 1);
            }

            _db.AylikYedekListeleri.RemoveRange(yedekListesiKayitlari);

            var ozelGunSet = await _db.OzelGunler
                .Where(x => x.Tarih >= startDate && x.Tarih < endDate)
                .Select(x => x.Tarih.Date)
                .ToHashSetAsync();

            var etutler = await _db.Etutler
                .Include(e => e.Personel)
                .Where(e => e.Tarih >= startDate && e.Tarih < endDate)
                .ToListAsync();

            foreach (var etut in etutler)
            {
                if (etut.Personel != null)
                {
                    if (etut.Tarih.DayOfWeek == DayOfWeek.Sunday)
                        etut.Personel.PazarSayisi = Math.Max(0, etut.Personel.PazarSayisi - 1);
                    else if (ozelGunSet.Contains(etut.Tarih.Date))
                        etut.Personel.OzelGunSayisi = Math.Max(0, etut.Personel.OzelGunSayisi - 1);
                    else
                        etut.Personel.HaftaIciSayisi = Math.Max(0, etut.Personel.HaftaIciSayisi - 1);
                }

                _db.Etutler.Remove(etut);
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<Personel>> PeekMonthlyYedekList(DateTime startDate)
        {
            int yil = startDate.Year;
            int ay = startDate.Month;

            var people = await _db.Personeller
                .Where(p => p.AktifMi)
                .ToListAsync();
            if (!people.Any()) return new List<Personel>();

            int seed = yil * 100 + ay;
            var prepared = people
                .Select(p => new
                {
                    Personel = p,
                    Primary = p.YedekSayisi,
                    TieBreaker = HashTie(seed, p.Id)
                })
                .OrderBy(x => x.Primary)
                .ThenBy(x => x.TieBreaker)
                .Select(x => x.Personel)
                .Take(Math.Min(1, people.Count))
                .ToList();

            return prepared;
        }
    
        public async Task ReplaceEtutWithYedekAsync(int etutId, int yedekPersonelId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var etut = await _db.Etutler.Include(e => e.Personel).FirstOrDefaultAsync(e => e.Id == etutId);
                if (etut == null) throw new InvalidOperationException("Etut bulunamadı.");

                var eskiPersonel = etut.Personel;
                var yeniPersonel = await _db.Personeller.Include(p => p.Mazeretler).FirstOrDefaultAsync(p => p.Id == yedekPersonelId);
                if (yeniPersonel == null) throw new InvalidOperationException("Yedek personel bulunamadı.");

                if (HasMazeret(yeniPersonel, etut.Tarih))
                    throw new InvalidOperationException("Yedek personel o gün mazeretli.");

                DecrementPersonelDayCounter(eskiPersonel, etut.Tarih);
                etut.PersonelId = yeniPersonel.Id;
                etut.Personel = yeniPersonel;
                IncrementPersonelDayCounter(yeniPersonel, etut.Tarih);
                yeniPersonel.YedekSayisi = Math.Max(0, yeniPersonel.YedekSayisi - 1);

                _db.Etutler.Update(etut);
                _db.Personeller.Update(eskiPersonel);
                _db.Personeller.Update(yeniPersonel);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task MoveEtutDateAsync(int etutId, DateTime targetDate)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var etut = await _db.Etutler.Include(e => e.Personel).FirstOrDefaultAsync(e => e.Id == etutId);
                if (etut == null) throw new InvalidOperationException("Etut bulunamadı.");

                var personel = etut.Personel;
                if (HasMazeret(personel, targetDate))
                    throw new InvalidOperationException("Personel hedef tarihte mazeretli.");

                var endDate = targetDate.Date;
                var originalDate = etut.Tarih.Date;

                if (originalDate == endDate) return;

                DecrementPersonelDayCounter(personel, etut.Tarih);
                etut.Tarih = targetDate;
                IncrementPersonelDayCounter(personel, etut.Tarih);

                _db.Etutler.Update(etut);
                _db.Personeller.Update(personel);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task SwapEtutPersonsAsync(int etutAId, int etutBId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var etutA = await _db.Etutler.Include(e => e.Personel).FirstOrDefaultAsync(e => e.Id == etutAId);
                var etutB = await _db.Etutler.Include(e => e.Personel).FirstOrDefaultAsync(e => e.Id == etutBId);
                if (etutA == null || etutB == null) throw new InvalidOperationException("Etut bulunamadı.");

                var personA = etutA.Personel;
                var personB = etutB.Personel;

                if (HasMazeret(personA, etutB.Tarih) || HasMazeret(personB, etutA.Tarih))
                    throw new InvalidOperationException("Kişilerden biri diğerinin gününde mazeretli.");

                DecrementPersonelDayCounter(personA, etutA.Tarih);
                DecrementPersonelDayCounter(personB, etutB.Tarih);

                var aPersonelId = personA.Id;
                var bPersonelId = personB.Id;

                etutA.PersonelId = bPersonelId;
                etutB.PersonelId = aPersonelId;

                etutA.Personel = personB;
                etutB.Personel = personA;

                IncrementPersonelDayCounter(personB, etutA.Tarih);
                IncrementPersonelDayCounter(personA, etutB.Tarih);

                _db.Etutler.UpdateRange(etutA, etutB);
                _db.Personeller.UpdateRange(personA, personB);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private void IncrementPersonelDayCounter(Personel p, DateTime tarih)
        {
            var ozelGunSet = _db.OzelGunler.Local.Where(x => x.Tarih.Date == tarih.Date).Select(x => x.Tarih.Date).ToHashSet();
            
            if (tarih.DayOfWeek == DayOfWeek.Sunday)
                p.PazarSayisi++;
            else if (ozelGunSet.Contains(tarih.Date))
                p.OzelGunSayisi++;
            else
                p.HaftaIciSayisi++;
        }

        private void DecrementPersonelDayCounter(Personel p, DateTime tarih)
        {
            var ozelGunSet = _db.OzelGunler.Local.Where(x => x.Tarih.Date == tarih.Date).Select(x => x.Tarih.Date).ToHashSet();
            
            if (tarih.DayOfWeek == DayOfWeek.Sunday)
                p.PazarSayisi = Math.Max(0, p.PazarSayisi - 1);
            else if (ozelGunSet.Contains(tarih.Date))
                p.OzelGunSayisi = Math.Max(0, p.OzelGunSayisi - 1);
            else
                p.HaftaIciSayisi = Math.Max(0, p.HaftaIciSayisi - 1);
        }
    
        public async Task<Etut> GetEtutWithPersonelAsync(int id)
        {
            return await _db.Etutler
                .Include(e => e.Personel)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Personel>> GetAvailableYedeklerForDateAsync(DateTime date)
        {
            var tarih = date.Date;
            int yil = tarih.Year;
            int ay = tarih.Month;

            var ayinYedekleri = await _db.AylikYedekListeleri
                .Include(a => a.Personel)
                .ThenInclude(p => p.Mazeretler)
                .Where(a => a.Yil == yil && a.Ay == ay)
                .OrderBy(a => a.Sira)
                .Select(a => a.Personel)
                .ToListAsync();

            if (!ayinYedekleri.Any())
            {
                var people = await _db.Personeller
                    .Where(p => p.AktifMi)
                    .Include(p => p.Mazeretler)
                    .ToListAsync();

                var atananIds = await _db.Etutler
                    .Where(e => e.Tarih.Date == tarih)
                    .Select(e => e.PersonelId)
                    .Distinct()
                    .ToListAsync();

                return people
                    .Where(p =>
                        p.YedekSayisi > 0
                        && !HasMazeret(p, date)
                        && !atananIds.Contains(p.Id)
                    )
                    .OrderBy(p => p.YedekSayisi)
                    .ThenBy(p => p.HaftaIciSayisi + p.PazarSayisi + p.OzelGunSayisi)
                    .ToList();
            }

            var atananIdsAy = await _db.Etutler
                .Where(e => e.Tarih.Date == tarih)
                .Select(e => e.PersonelId)
                .Distinct()
                .ToListAsync();

            var result = ayinYedekleri
                .Where(p =>
                    p != null
                    && p.AktifMi
                    && !HasMazeret(p, date)
                    && !atananIdsAy.Contains(p.Id)
                )
                .ToList();

            return result;
        }

        private bool HasMazeret(Personel p, DateTime d) =>
            p.Mazeretler.Any(m => m.Baslangic.Date <= d.Date && m.Bitis.Date >= d.Date);

        private bool IsEtutGunu(DateTime d) =>
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Sunday }
                .Contains(d.DayOfWeek);
    }
}
