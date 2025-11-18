namespace Etutlist.Models
{
    public class Hoca
    {
  public int Id { get; set; }
   public string Rutbe { get; set; }
        public string AdSoyad { get; set; }
        public bool AktifMi { get; set; }

        // Foreign key
  public int FakulteId { get; set; }
      public Fakulte Fakulte { get; set; }

  // Ders Programındaki dersler (haftalık program)
 public ICollection<DersProgrami> Dersler { get; set; }
   public ICollection<TelafiDers> TelafiDersler { get; set; }

        // Hoca-Ders ilişkisi (hocanın verebileceği dersler)
        public ICollection<HocaDers> HocaDersler { get; set; }

        // Hesaplanan property - Haftalık ders yükü (toplam ders saati)
     public int DersYuku => Dersler?.Count ?? 0;
    }
}
