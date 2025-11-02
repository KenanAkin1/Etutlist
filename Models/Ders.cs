namespace Etutlist.Models
{
    public class Ders
  {
        public int Id { get; set; }
   public string DersAdi { get; set; }
        
// Navigation
     public ICollection<DersProgrami> DersProgramlari { get; set; }
      
        // Hoca-Ders iliþkisi (bu dersi verebilecek hocalar)
        public ICollection<HocaDers> DersHocalari { get; set; }
    }
}
