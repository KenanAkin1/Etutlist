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

         // Önce bu ay için zaten plan var mý kontrol et
            bool planExists = await _db.Etutler.AnyAsync(e => e.Tarih >= startDate && e.Tarih < endDate);
            
         // Eðer plan varsa, önce eski planý sil (bu yedek sayýlarýný da geri alýr)
      if (planExists)
   {
     await DeleteMonthlyPlan(startDate);
            }

         var people = await _db.Personeller
       .Include(p => p.Mazeretler)
        .ToListAsync();

       var ozelGunSet = await _db.OzelGunler
        .Where(x => x.Tarih >= startDate && x.Tarih < endDate)
   .Select(x => x.Tarih.Date)
            .ToHashSetAsync();

     var ozelDortluIds = new List<int> { 28, 24, 22, 62 };
    var rnd = new Random();

    // Planlanabilir günler
            var allDays = Enumerable.Range(0, (endDate - startDate).Days)
       .Select(i => startDate.AddDays(i))
    .Where(d => IsEtutGunu(d))
   .ToList();

 var pazarlar = allDays.Where(d => d.DayOfWeek == DayOfWeek.Sunday).ToList();
          var ozelGunler = allDays.Where(d => ozelGunSet.Contains(d.Date)).ToList();
     var haftaIci = allDays.Except(pazarlar).Except(ozelGunler).ToList();

         // Özel dörtlü listesi
       var ozelDortlu = people.Where(p => ozelDortluIds.Contains(p.Id)).ToList();

      // --- Yenilenmiþ hedef gün seçimi: sadece tüm dört üyenin MAZERETSÝZ olduðu günleri al ---
        DateTime? hedefGunNullable = null;
            var adaylerIcindekiGunler = allDays
     .Where(d => ozelDortlu.All(p => !HasMazeret(p, d)))
        .ToList();

            // Öncelik: hafta içi günleri; yoksa özel günler; yoksa pazarlar.
 var uygunGunler = adaylerIcindekiGunler.Where(d => !pazarlar.Contains(d) && !ozelGunler.Contains(d)).ToList();
     if (!uygunGunler.Any()) uygunGunler = adaylerIcindekiGunler.Where(d => ozelGunler.Contains(d)).ToList();
 if (!uygunGunler.Any()) uygunGunler = adaylerIcindekiGunler.Where(d => pazarlar.Contains(d)).ToList();

 if (uygunGunler.Any())
{
                hedefGunNullable = uygunGunler[rnd.Next(uygunGunler.Count)];
 }

            bool ozelDortluAtandi = false;

    foreach (var d in allDays)
    {
    var adaylar = people.Where(p => !HasMazeret(p, d)).ToList();
 bool gunTipiPazar = d.DayOfWeek == DayOfWeek.Sunday;
             bool gunTipiOzel = ozelGunSet.Contains(d.Date);

           if (!ozelDortluAtandi && hedefGunNullable.HasValue && d.Date == hedefGunNullable.Value.Date)
  {
            var ozelDortluMusait = adaylar.Where(p => ozelDortluIds.Contains(p.Id)).ToList();
            if (ozelDortluMusait.Count == ozelDortluIds.Count)
  {
    var secilenler = new List<Personel>(ozelDortluMusait);
            int eksik = 4 - secilenler.Count;
     if (eksik > 0)
          {
           var digerAdaylar = adaylar.Where(p => !ozelDortluIds.Contains(p.Id)).ToList();
      var tamamlayici = SecimKurallari(digerAdaylar, d, ozelGunSet).Take(eksik);
    secilenler.AddRange(tamamlayici);
}

              KaydetVeSayacArtir(d, secilenler, ozelGunSet);
    ozelDortluAtandi = true;
    await _db.SaveChangesAsync();
           }
         continue;
                }

          List<Personel> secilecek = new List<Personel>();

     if (gunTipiPazar || gunTipiOzel)
       {
          var nonQuartet = adaylar.Where(p => !ozelDortluIds.Contains(p.Id)).ToList();
           secilecek.AddRange(SecimKurallari(nonQuartet, d, ozelGunSet).Take(4));
        }
         else
                {
 secilecek = SecimKurallari(adaylar, d, ozelGunSet).Take(4).ToList();
          }

       if (!secilecek.Any()) continue;

    KaydetVeSayacArtir(d, secilecek, ozelGunSet);
          await _db.SaveChangesAsync();
}

          // Döngü bittiðinde otomatik yedekleri üret (ay bazlý deterministik)
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

        // ? YENÝ: Yedek listesini VERÝTABANINA KAYDET
        public async Task<List<Personel>> GenerateMonthlyYedekList(DateTime startDate, int yedekSayisi = 15)
        {
         int yil = startDate.Year;
            int ay = startDate.Month;
    int seed = yil * 100 + ay;

 var people = await _db.Personeller.ToListAsync();

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
  .Take(Math.Min(yedekSayisi, people.Count))
.ToList();

     // YedekSayisi'ný artýr
            foreach (var p in prepared)
      p.YedekSayisi++;

          _db.Personeller.UpdateRange(prepared);

            // ? VERÝTABANINA KAYDET (hangi personellerin yedek olduðunu)
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

        // ?? SADECE ÖNÝZLEME (veritabanýndan oku)
        public async Task<List<Personel>> PeekMonthlyYedekList(DateTime startDate, int yedekSayisi = 15)
  {
         int yil = startDate.Year;
         int ay = startDate.Month;

            // VERÝTABANINDAN yedek listesini çek
         var yedekListesi = await _db.AylikYedekListeleri
        .Include(a => a.Personel)
          .Where(a => a.Yil == yil && a.Ay == ay)
 .OrderBy(a => a.Sira)
     .Select(a => a.Personel)
          .ToListAsync();

      // Eðer veritabanýnda kayýt varsa, onlarý döndür
      if (yedekListesi.Any())
       {
  return yedekListesi;
         }

            // Yoksa, simüle et (ama kaydetme)
var people = await _db.Personeller.AsNoTracking().ToListAsync();
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
     .Take(Math.Min(yedekSayisi, people.Count))
        .ToList();

            return prepared;
        }

      public async Task<List<Personel>> GetOrGenerateYedekList(DateTime startDate, int yedekSayisi = 15)
        {
            int yil = startDate.Year;
            int ay = startDate.Month;

    // Ayda hiç plan yoksa otomatik üret
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

        // ? YENÝ: Planý silerken VERÝTABANINDAN yedek listesini oku
        public async Task DeleteMonthlyPlan(DateTime startDate)
        {
    var endDate = startDate.AddMonths(1);
            int yil = startDate.Year;
            int ay = startDate.Month;

            // ? VERÝTABANINDAN yedek listesini çek
            var yedekListesiKayitlari = await _db.AylikYedekListeleri
              .Include(a => a.Personel)
           .Where(a => a.Yil == yil && a.Ay == ay)
                .ToListAsync();

    // ? Bu kayýtlardaki personellerin YedekSayisi'ný azalt
       foreach (var kayit in yedekListesiKayitlari)
       {
       kayit.Personel.YedekSayisi = Math.Max(0, kayit.Personel.YedekSayisi - 1);
        }

      // ? Yedek listesi kayýtlarýný sil
            _db.AylikYedekListeleri.RemoveRange(yedekListesiKayitlari);

            // ?? Etütleri sil ve sayaçlarý azalt
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

       var people = await _db.Personeller.ToListAsync();
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
            if (etut == null) throw new InvalidOperationException("Etut bulunamadý.");

      var eskiPersonel = etut.Personel;
 var yeniPersonel = await _db.Personeller.Include(p => p.Mazeretler).FirstOrDefaultAsync(p => p.Id == yedekPersonelId);
           if (yeniPersonel == null) throw new InvalidOperationException("Yedek personel bulunamadý.");

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
    if (etut == null) throw new InvalidOperationException("Etut bulunamadý.");

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
      if (etutA == null || etutB == null) throw new InvalidOperationException("Etut bulunamadý.");

    var personA = etutA.Personel;
     var personB = etutB.Personel;

        if (HasMazeret(personA, etutB.Tarih) || HasMazeret(personB, etutA.Tarih))
         throw new InvalidOperationException("Kiþilerden biri diðerinin gününde mazeretli.");

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
            if (tarih.DayOfWeek == DayOfWeek.Sunday) p.PazarSayisi++;
            else if (_db.OzelGunler.Local.Any() ? _db.OzelGunler.Local.Any(x => x.Tarih.Date == tarih.Date) : false)
          {
        p.OzelGunSayisi++;
     }
       else
          {
  p.HaftaIciSayisi++;
            }
        }

        private void DecrementPersonelDayCounter(Personel p, DateTime tarih)
        {
          if (tarih.DayOfWeek == DayOfWeek.Sunday) p.PazarSayisi = Math.Max(0, p.PazarSayisi - 1);
     else if (_db.OzelGunler.Local.Any() ? _db.OzelGunler.Local.Any(x => x.Tarih.Date == tarih.Date) : false)
            {
                p.OzelGunSayisi = Math.Max(0, p.OzelGunSayisi - 1);
          }
  else
  {
        p.HaftaIciSayisi = Math.Max(0, p.HaftaIciSayisi - 1);
    }
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

          var people = await _db.Personeller
                .Include(p => p.Mazeretler)
        .ToListAsync();

   var atananIds = await _db.Etutler
     .Where(e => e.Tarih.Date == tarih)
  .Select(e => e.PersonelId)
    .Distinct()
         .ToListAsync();

        var ozelGunSet = await _db.OzelGunler
.Select(x => x.Tarih.Date)
       .ToHashSetAsync();

          var result = people
       .Where(p =>
      p.YedekSayisi > 0
    && !HasMazeret(p, date)
      && !atananIds.Contains(p.Id)
    )
       .OrderBy(p => p.YedekSayisi)
       .ThenBy(p => p.HaftaIciSayisi + p.PazarSayisi + p.OzelGunSayisi)
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
