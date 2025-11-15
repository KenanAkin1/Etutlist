namespace Etutlist.Models
{
    public class Tabur
    {
        public int Id { get; set; }
        public int FakulteId { get; set; }
        public Fakulte Fakulte { get; set; }
        public string TaburAdi { get; set; }
        public int MinKisimNo { get; set; }
        public int MaxKisimNo { get; set; }
        public ICollection<TaburTelafiAyarlari> TelafiAyarlari { get; set; } = new List<TaburTelafiAyarlari>();
    }
}
