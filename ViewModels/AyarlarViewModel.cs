using Etutlist.Models;

namespace Etutlist.ViewModels
{
    public class AyarlarViewModel
    {
        public List<Fakulte> Fakulteler { get; set; } = new();
        public List<Hoca> Hocalar { get; set; } = new();
        public List<Ders> Dersler { get; set; } = new();
    }
}
