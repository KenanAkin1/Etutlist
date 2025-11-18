using Etutlist.Models;

namespace Etutlist.Controllers
{
    public class DersDetayAyarlarViewModel
    {
  public Ders Ders { get; set; }
 public List<Hoca> DersHocalari { get; set; } = new();
   public List<DersProgrami> DersProgramlari { get; set; } = new();
    }
}
