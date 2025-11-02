namespace Etutlist.Models
{
    public class Etut
    {
        public int Id { get; set; }
        public int PersonelId { get; set; }
        public Personel Personel { get; set; }
        public DateTime Tarih { get; set; }
        public string Tip { get; set; }
        public int? SinifNo { get; set; }
    }

}
