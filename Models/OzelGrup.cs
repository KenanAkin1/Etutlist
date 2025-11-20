namespace Etutlist.Models
{
    public class OzelGrup
    {
        public int Id { get; set; }
        public string GrupAdi { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public bool AktifMi { get; set; } = true;
        
        /// <summary>
        /// true: Grup ortalamasýna göre ata (varsayýlan)
        /// false: Gruptaki en fazla tutan kiþiye göre ata
        /// </summary>
        public bool OrtalamaKullan { get; set; } = true;
        
        // Navigation
        public ICollection<OzelGrupUyesi> Uyeler { get; set; } = new List<OzelGrupUyesi>();
    }

    public class OzelGrupUyesi
    {
        public int Id { get; set; }
        public int OzelGrupId { get; set; }
        public int PersonelId { get; set; }
        
        // Navigation
        public OzelGrup OzelGrup { get; set; } = null!;
        public Personel Personel { get; set; } = null!;
    }
}
