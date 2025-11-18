using Etutlist.Models;

namespace Etutlist.Controllers
{
    public class DersDetayViewModel
    {
   public DersProgrami Ders { get; set; }
        public List<Hoca> AyniDersHocalar { get; set; } = new();
   public List<DersProgrami> HocaDersleri { get; set; } = new();
  public List<TelafiDers> Telafiler { get; set; } = new();
    }
}
