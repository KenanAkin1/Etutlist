namespace Etutlist.Models
{
    /// <summary>
    /// Tabur için telafi dersleri yapılabilecek günler ve saatler
    /// </summary>
    public class TaburTelafiAyarlari
    {
        public int Id { get; set; }
        public int TaburId { get; set; }
        public Tabur Tabur { get; set; }
        
        /// <summary>
        /// Telafi derslerinin YAPILABİLECEĞİ gün
        /// Örnek: "Pazartesi" ? Pazartesi günü bu tabur için telafi dersleri yapılabilir
        /// </summary>
        public string TelafiYapilacakGun { get; set; }
        
        /// <summary>
        /// Telafi derslerinin kaçıncı dersten başlayacağı
        /// Örnek: 7 ? 7. ders saatinden itibaren telafi yapılabilir
        /// </summary>
        public int TelafiBaslamaSaati { get; set; }
        
        /// <summary>
        /// Telafi derslerinin en fazla kaçıncı derse kadar yapılacağı
        /// Varsayılan: 9 (9. derse kadar)
        /// </summary>
        public int TelafiMaxBitisSaati { get; set; } = 9;
        
        /// <summary>
        /// Bu günde atlanacak ders saatleri (virgülle ayrılmış)
        /// Örnek: "1,2" ? 1. ve 2. ders saatleri telafi için kullanılmaz
        /// Boş ise tüm saatler kullanılabilir
        /// </summary>
        public string? TelafiYapilamayacakDersSaatleri { get; set; }
    }
}
