using System.ComponentModel.DataAnnotations;

namespace Etutlist.Models
{
    public class TelafiDers
    {
        public int Id { get; set; }

        [Required]
        public int DersProgramiId { get; set; }

  [Required]
        public int YedekHocaId { get; set; }

      [Required]
public int FakulteId { get; set; }

        public int? KisimNo { get; set; }  // Telafi yapýlacak kýsým (opsiyonel)

        [Required]
        [DataType(DataType.Date)]
        public DateTime TelafiTarihi { get; set; }

        [Required]
        public TimeSpan BaslangicSaat { get; set; }

        [Required]
        public TimeSpan BitisSaat { get; set; }

    [Required]
        public string TelafiTuru { get; set; }  // Telafi, Ýkame, Birleþtirme

        [Required]
    public string TelafiNedeni { get; set; }

        public string? Aciklama { get; set; }

        public bool Onaylandi { get; set; }
  
        public bool CiktiAlindi { get; set; } = false;  // Excel çýktýsý alýndý mý?

   // Navigation properties
        public DersProgrami DersProgrami { get; set; }
        public Hoca YedekHoca { get; set; }
    public Fakulte Fakulte { get; set; }
    }

    public static class TelafiTurleri
    {
        public static readonly List<string> TumTurler = new()
        {
   "Telafi",
    "Ýkame",
            "Birleþtirme"
        };
    }
}
