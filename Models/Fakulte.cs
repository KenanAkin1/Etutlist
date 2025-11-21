using System.ComponentModel.DataAnnotations.Schema;

namespace Etutlist.Models
{
    public class Fakulte
    {
        public int Id { get; set; }
        public string Ad { get; set; }

        /// <summary>
        /// Pazartesi'den Cuma'ya günlük ders saati limitleri.
        /// Format: "7,7,6,7,4" (Virgülle ayrılmış 5 sayı)
        /// </summary>
        public string? GunlukDersSaatleri { get; set; }

        /// <summary>
        /// Veritabanındaki string alanı diziye çeviren yardımcı property.
        /// Veritabanına kaydedilmez ([NotMapped]).
        /// </summary>
        [NotMapped]
        public int[] SaatlerDizisi
        {
            get
            {
                if (string.IsNullOrEmpty(GunlukDersSaatleri))
                    return new int[] { 9, 9, 9, 9, 9 }; // Varsayılan: Her gün 9 ders

                // String'i virgülden böl ve int dizisine çevir
                return GunlukDersSaatleri.Split(',')
                    .Select(x => int.TryParse(x, out int v) ? v : 9)
                    .ToArray();
            }
        }

        // Navigation Properties
        public ICollection<Hoca> Hocalar { get; set; }
        public ICollection<DersProgrami> DersProgramlari { get; set; }
        public ICollection<TelafiDers> TelafiDersler { get; set; }
        public ICollection<Tabur> Taburlar { get; set; }
    }
}