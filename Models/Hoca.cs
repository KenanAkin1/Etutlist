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

  // Ders Programýndaki dersler (haftalýk program)
 public ICollection<DersProgrami> Dersler { get; set; }
   public ICollection<TelafiDers> TelafiDersler { get; set; }

        // Hoca-Ders iliþkisi (hocanýn verebileceði dersler)
        public ICollection<HocaDers> HocaDersler { get; set; }

        // Hesaplanan property - Haftalýk ders yükü (toplam ders saati)
     public int DersYuku => Dersler?.Count ?? 0;
    }
}
