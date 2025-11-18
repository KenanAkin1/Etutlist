using Etutlist.Models;

namespace Etutlist.ViewModels
{
    public class PersonelIndexViewModel
    {
    public List<Personel> AktifPersoneller { get; set; } = new();
        public List<Personel> PasifPersoneller { get; set; } = new();
    }
}
