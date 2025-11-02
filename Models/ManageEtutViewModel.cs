// Models/ManageEtutViewModel.cs
namespace Etutlist.Models
{
    public class ManageEtutViewModel
    {
        public Etut Etut { get; set; }
        public List<Personel> MusaitYedekler { get; set; } = new();
        public List<Etut> TumEtutler { get; set; } = new();
    }
}