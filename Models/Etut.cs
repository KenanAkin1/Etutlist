namespace Etutlist.Models
{
    public class Etut
    {
public int Id { get; set; }
        public int? PersonelId { get; set; } // ? Nullable (personel silinince NULL)
 public Personel? Personel { get; set; } // ? Nullable navigation
        
        // ? YENÝ: Personel silinse bile ad/rütbe bilgisi korunsun
        public string? PersonelAd { get; set; }
        public string? PersonelRutbe { get; set; }

   public DateTime Tarih { get; set; }
        public string Tip { get; set; }
   public int? SinifNo { get; set; }
    }
}
