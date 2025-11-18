namespace Etutlist.Models
{
    public class DersProgrami
  {
        public int Id { get; set; }
   public int FakulteId { get; set; }
     public int KisimNo { get; set; }  // Derslik yerine sadece Kısım No (1, 2, 3...)
      public int? DersId { get; set; }  // Ders (opsiyonel)
        public int HocaId { get; set; }
        public string DersAdi { get; set; }
        public string DersKodu { get; set; }
        public string DersGunu { get; set; }  // Pazartesi, Salı, vb.
      public int DersSaati { get; set; }  // 1, 2, 3, ..., 8

  // Navigation Properties
     public Fakulte Fakulte { get; set; }
        public Ders Ders { get; set; }
   public Hoca Hoca { get; set; }
    public ICollection<TelafiDers> TelafiDersler { get; set; }
    }

    public static class DersSaatleri
    {
 public static readonly Dictionary<int, string> Saatler = new Dictionary<int, string>
   {
 { 1, "1. Saat (08:30-09:10)" },
    { 2, "2. Saat (09:20-10:00)" },
   { 3, "3. Saat (10:10-10:50)" },
   { 4, "4. Saat (11:00-11:40)" },
{ 5, "5. Saat (12:20-13:00)" },
       { 6, "6. Saat (13:10-13:50)" },
     { 7, "7. Saat (14:00-14:40)" },
          { 8, "8. Saat (14:50-15:30)" }
  };
    }

  public static class GunSabitler
 {
   public static readonly List<string> HaftaGunleri = new List<string>
  {
        "Pazartesi",
       "Salı",
   "Çarşamba",
  "Perşembe",
 "Cuma"
 };
  }
}
