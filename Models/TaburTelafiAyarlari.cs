namespace Etutlist.Models
{
    public class TaburTelafiAyarlari
    {
        public int Id { get; set; }
        public int TaburId { get; set; }
        public Tabur Tabur { get; set; }
        public string TelafiYapilamayacakGun { get; set; }
        public int TelafiBaslamaSaati { get; set; }
        public int TelafiMaxBitisSaati { get; set; } = 9;
        public string? TelafiYapilamayacakDersSaatleri { get; set; }
    }
}
