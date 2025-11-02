namespace Etutlist.Models
{
    public class AylikYedekListesi
  {
    public int Id { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }
   public int PersonelId { get; set; }
      public Personel Personel { get; set; }
        public int Sira { get; set; } // Yedek listesindeki sırası (1-15)
  }
}
