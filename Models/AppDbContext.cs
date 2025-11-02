using Etutlist.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Models
{
    public class AppDbContext : DbContext
    {
   public DbSet<Personel> Personeller { get; set; }
        public DbSet<Etut> Etutler { get; set; }
        public DbSet<OzelGun> OzelGunler { get; set; }
   public DbSet<Mazeret> Mazeretler { get; set; }
        public DbSet<Hoca> Hocalar { get; set; }
  public DbSet<DersProgrami> DersProgrami { get; set; }
        public DbSet<TelafiDers> TelafiDersler { get; set; }
      public DbSet<Fakulte> Fakulteler { get; set; }
        public DbSet<Ders> Dersler { get; set; }
   public DbSet<HocaDers> HocaDersler { get; set; }
        public DbSet<AylikYedekListesi> AylikYedekListeleri { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new PersonelConfiguration());
            modelBuilder.ApplyConfiguration(new EtutConfiguration());
            modelBuilder.ApplyConfiguration(new OzelGunConfiguration());
   modelBuilder.ApplyConfiguration(new MazeretConfiguration());

            // HocaDers many-to-many iliþkisi
            modelBuilder.Entity<HocaDers>()
          .HasKey(hd => new { hd.HocaId, hd.DersId });

   modelBuilder.Entity<HocaDers>()
         .HasOne(hd => hd.Hoca)
          .WithMany(h => h.HocaDersler)
  .HasForeignKey(hd => hd.HocaId);

            modelBuilder.Entity<HocaDers>()
 .HasOne(hd => hd.Ders)
    .WithMany(d => d.DersHocalari)
             .HasForeignKey(hd => hd.DersId);

  // TelafiDers iliþkileri
            modelBuilder.Entity<TelafiDers>()
     .HasOne(t => t.YedekHoca)
   .WithMany()
           .HasForeignKey(t => t.YedekHocaId)
       .OnDelete(DeleteBehavior.NoAction);

    modelBuilder.Entity<TelafiDers>()
         .HasOne(t => t.DersProgrami)
     .WithMany(d => d.TelafiDersler)
          .HasForeignKey(t => t.DersProgramiId)
      .OnDelete(DeleteBehavior.NoAction);

  modelBuilder.Entity<TelafiDers>()
 .HasOne(t => t.Fakulte)
     .WithMany(f => f.TelafiDersler)
        .HasForeignKey(t => t.FakulteId)
   .OnDelete(DeleteBehavior.NoAction);

     // DersProgrami iliþkileri
       modelBuilder.Entity<DersProgrami>()
   .HasOne(d => d.Fakulte)
.WithMany(f => f.DersProgramlari)
                .HasForeignKey(d => d.FakulteId)
       .OnDelete(DeleteBehavior.NoAction);

          modelBuilder.Entity<DersProgrami>()
        .HasOne(dp => dp.Hoca)
     .WithMany(h => h.Dersler)
       .HasForeignKey(dp => dp.HocaId)
    .OnDelete(DeleteBehavior.NoAction);

  // Ders iliþkileri (opsiyonel)
            modelBuilder.Entity<DersProgrami>()
.HasOne(dp => dp.Ders)
    .WithMany(d => d.DersProgramlari)
    .HasForeignKey(dp => dp.DersId)
                .OnDelete(DeleteBehavior.SetNull);

            // AylikYedekListesi iliþkisi
         modelBuilder.Entity<AylikYedekListesi>()
      .HasOne(a => a.Personel)
     .WithMany()
    .HasForeignKey(a => a.PersonelId)
           .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: Ayný ay-yýl için ayný personel birden fazla olamaz
            modelBuilder.Entity<AylikYedekListesi>()
      .HasIndex(a => new { a.Yil, a.Ay, a.PersonelId })
       .IsUnique();
        }
    }
}
