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

 // Telafi önerisi al - GELÝÞTÝRÝLMÝÞ
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

// 1. TELAFÝ kontrolü: Ayný hoca müsait mi?
       bool hocaMusait = await IsHocaMusaitAsync(ders.HocaId, telafiTarihi, baslangicSaat, bitisSaat);
        
    if (hocaMusait)
       {
      oneri.OnerilenTur = "Telafi";
            oneri.Aciklama = $"Telafi dersi yapýlabilir. Ayný hoca ({ders.Hoca.AdSoyad}) dersi verebilir.";
    oneri.MusaitYedekHocalar = new List<Hoca> { ders.Hoca };
  return oneri;
         }

   // 2. ÝKAME kontrolü: Baþka hocalar müsait mi?
            var musaitHocalar = await GetMusaitHocalarAsync(ders.FakulteId, ders.HocaId, telafiTarihi, baslangicSaat, bitisSaat, ders.DersId);
     
     if (musaitHocalar.Any())
       {
   oneri.OnerilenTur = "Ýkame";
  oneri.Aciklama = $"Ýkame dersi önerilir. {musaitHocalar.Count} müsait hoca bulundu.";
     oneri.MusaitYedekHocalar = musaitHocalar;
       return oneri;
       }

       // 3. BÝRLEÞTÝRME: Hiç müsait hoca yok
      oneri.OnerilenTur = "Birleþtirme";
   oneri.Aciklama = "Uygun hoca bulunamadý. Dersin baþka bir bölümle birleþtirilmesi önerilir.";
oneri.MusaitYedekHocalar = new List<Hoca>();
      
  // Birleþtirilebilecek dersler
   oneri.BirlestirilebilirDersler = await GetBirlestirilebilirDerslerAsync(dersProgramiId, telafiTarihi);

     return oneri;
        }

        // Hocanýn müsait olup olmadýðýný kontrol et
   private async Task<bool> IsHocaMusaitAsync(int hocaId, DateTime tarih, TimeSpan baslangic, TimeSpan bitis)
      {
  // Normal ders programýnda mý?
   var normalDersiVar = await _context.DersProgrami.AnyAsync(d =>
    d.HocaId == hocaId &&
          d.DersGunu == GetGunAdi(tarih) &&
       d.DersSaati == GetSaatIndex(baslangic));

   if (normalDersiVar)
     return false;

      // Baþka telafi dersinde mi?
    var telafideGorevli = await _context.TelafiDersler.AnyAsync(t =>
       t.YedekHocaId == hocaId &&
   t.TelafiTarihi.Date == tarih.Date &&
       ((t.BaslangicSaat < bitis && t.BitisSaat > baslangic)));

  return !telafideGorevli;
 }

 // Müsait hocalarý getir
  private async Task<List<Hoca>> GetMusaitHocalarAsync(int fakulteId, int mevcutHocaId, DateTime tarih, TimeSpan baslangic, TimeSpan bitis, int? dersId)
   {
     // Tüm aktif hocalarý al
   var tumHocalar = await _context.Hocalar
     .Include(h => h.HocaDersler)
    .Where(h => h.AktifMi && h.FakulteId == fakulteId && h.Id != mevcutHocaId)
    .ToListAsync();

    var musaitHocalar = new List<Hoca>();

       foreach (var hoca in tumHocalar)
{
// Eðer ders belirtilmiþse, bu dersi verebiliyor mu?
  if (dersId.HasValue)
   {
        bool dersVerebilir = await _context.HocaDersler
  .AnyAsync(hd => hd.HocaId == hoca.Id && hd.DersId == dersId.Value);
     
     if (!dersVerebilir)
continue;
   }

   // Müsait mi kontrol et
         bool musait = await IsHocaMusaitAsync(hoca.Id, tarih, baslangic, bitis);
   
     if (musait)
   {
         musaitHocalar.Add(hoca);
  }
  }

return musaitHocalar.OrderBy(h => h.AdSoyad).ToList();
   }

 // Birleþtirilebilir dersleri bul
 private async Task<List<DersProgrami>> GetBirlestirilebilirDerslerAsync(int dersProgramiId, DateTime telafiTarihi)
    {
    var anaDers = await _context.DersProgrami
   .Include(d => d.Hoca)
     .Include(d => d.Fakulte)
          .FirstOrDefaultAsync(d => d.Id == dersProgramiId);

     if (anaDers == null)
     return new List<DersProgrami>();

   // Ayný fakülte, ayný gün, yakýn saatlerdeki dersler
        var gunAdi = GetGunAdi(telafiTarihi);
         
   var birlestirilebilirler = await _context.DersProgrami
        .Include(d => d.Hoca)
     .Where(d => 
      d.Id != dersProgramiId &&
d.FakulteId == anaDers.FakulteId &&
      d.DersGunu == gunAdi &&
    d.DersId == anaDers.DersId) // Ayný ders
   .ToListAsync();

  return birlestirilebilirler;
  }

     // Telafi dersi oluþtur
   public async Task<(bool Success, string Message)> CreateTelafiDersAsync(TelafiDers telafiDers)
        {
        // Hoca meþgul mü kontrol et
   var hocaMesgul = await _context.TelafiDersler.AnyAsync(t =>
 t.YedekHocaId == telafiDers.YedekHocaId &&
   t.TelafiTarihi.Date == telafiDers.TelafiTarihi.Date &&
     ((t.BaslangicSaat < telafiDers.BitisSaat && t.BitisSaat > telafiDers.BaslangicSaat)));

        if (hocaMesgul)
     return (false, "Seçilen hoca bu saatte baþka bir telafi dersinde görevli.");

       // Normal programda dersi var mý?
   var normalDersi = await _context.DersProgrami.AnyAsync(d =>
       d.HocaId == telafiDers.YedekHocaId &&
 d.DersGunu == GetGunAdi(telafiDers.TelafiTarihi) &&
   d.DersSaati == GetSaatIndex(telafiDers.BaslangicSaat));

 if (normalDersi)
   return (false, "Seçilen hocanýn bu saatte normal dersi var.");

   _context.TelafiDersler.Add(telafiDers);
  await _context.SaveChangesAsync();

 return (true, "Telafi dersi baþarýyla oluþturuldu.");
  }

   // Telafi derslerini listele
 public async Task<List<TelafiDers>> GetTelafiDerslerAsync(int? fakulteId = null)
   {
     var query = _context.TelafiDersler
      .Include(t => t.DersProgrami)
    .ThenInclude(d => d.Hoca)
      .Include(t => t.DersProgrami)
    .ThenInclude(d => d.Ders)
    .Include(t => t.YedekHoca)
      .Include(t => t.Fakulte)
      .OrderByDescending(t => t.TelafiTarihi)
      .AsQueryable();

  if (fakulteId.HasValue)
    query = query.Where(t => t.FakulteId == fakulteId);

       return await query.ToListAsync();
    }

   // Telafi detayý getir
   public async Task<TelafiDers> GetTelafiDersAsync(int id)
        {
   return await _context.TelafiDersler
    .Include(t => t.DersProgrami)
     .ThenInclude(d => d.Hoca)
        .Include(t => t.DersProgrami)
     .ThenInclude(d => d.Ders)
 .Include(t => t.DersProgrami)
     .ThenInclude(d => d.Fakulte)
        .Include(t => t.YedekHoca)
     .Include(t => t.Fakulte)
   .FirstOrDefaultAsync(t => t.Id == id);
   }

   // Telafi sil
  public async Task<(bool Success, string Message)> DeleteTelafiDersAsync(int id)
     {
      var telafi = await _context.TelafiDersler.FindAsync(id);
         
     if (telafi == null)
      return (false, "Telafi dersi bulunamadý.");

     _context.TelafiDersler.Remove(telafi);
 await _context.SaveChangesAsync();

      return (true, "Telafi dersi silindi.");
    }

        

   // Telafi güncelle
        public async Task<(bool Success, string Message)> UpdateTelafiDersAsync(TelafiDers telafiDers)
        {
var mevcutTelafi = await _context.TelafiDersler.FindAsync(telafiDers.Id);
            if (mevcutTelafi == null)
          return (false, "Telafi dersi bulunamadý.");

            // Hoca meþgul mü kontrol et (kendisi hariç)
  var hocaMesgul = await _context.TelafiDersler.AnyAsync(t =>
 t.Id != telafiDers.Id &&
           t.YedekHocaId == telafiDers.YedekHocaId &&
         t.TelafiTarihi.Date == telafiDers.TelafiTarihi.Date &&
   ((t.BaslangicSaat < telafiDers.BitisSaat && t.BitisSaat > telafiDers.BaslangicSaat)));

            if (hocaMesgul)
      return (false, "Seçilen hoca bu saatte baþka bir telafi dersinde görevli.");

            // Güncelle
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
        }// Telafi onayla
    public async Task<(bool Success, string Message)> OnaylaTelafiDersAsync(int id)
   {
     var telafi = await _context.TelafiDersler.FindAsync(id);
            
      if (telafi == null)
   return (false, "Telafi dersi bulunamadý.");

        telafi.Onaylandi = true;
   await _context.SaveChangesAsync();

       return (true, "Telafi dersi onaylandý.");
  }

     // Yardýmcý metodlar
   private string GetGunAdi(DateTime tarih)
   {
    return tarih.DayOfWeek switch
     {
   DayOfWeek.Monday => "Pazartesi",
    DayOfWeek.Tuesday => "Salý",
   DayOfWeek.Wednesday => "Çarþamba",
    DayOfWeek.Thursday => "Perþembe",
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
        public string OnerilenTur { get; set; } // "Telafi", "Ýkame", "Birleþtirme"
        public string Aciklama { get; set; }
        public List<Hoca> MusaitYedekHocalar { get; set; } = new();
        public List<DersProgrami> BirlestirilebilirDersler { get; set; } = new();
    }
}

