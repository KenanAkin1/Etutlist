namespace Etutlist.Models
{
    public class Personel
    {
   public int Id { get; set; }
        public string Ad { get; set; }
        public string Rutbe { get; set; }
        public int YedekSayisi { get; set; }
        public int PazarSayisi { get; set; }
   public int OzelGunSayisi { get; set; }
      public int HaftaIciSayisi { get; set; }
        
        // ? SOFT DELETE
        public bool AktifMi { get; set; } = true;
    
        public ICollection<Etut> Etutler { get; set; } = new List<Etut>();
        public ICollection<Mazeret> Mazeretler { get; set; } = new List<Mazeret>();
    }
}
