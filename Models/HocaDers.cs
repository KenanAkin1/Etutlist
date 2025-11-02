namespace Etutlist.Models
{
    // Hoca-Ders many-to-many iliþkisi için ara tablo
    public class HocaDers
    {
   public int HocaId { get; set; }
    public Hoca Hoca { get; set; }

        public int DersId { get; set; }
  public Ders Ders { get; set; }
    }
}
