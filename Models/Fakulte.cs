namespace Etutlist.Models
{
    public class Fakulte
    {
   public int Id { get; set; }
  public string Ad { get; set; }  // ASEM, SUEM

   // Navigation
 public ICollection<Hoca> Hocalar { get; set; }
   public ICollection<DersProgrami> DersProgramlari { get; set; }
    public ICollection<TelafiDers> TelafiDersler { get; set; }
    }
}
