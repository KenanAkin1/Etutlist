using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etutlist.Models;

namespace Etutlist.Configurations
{
    public class PersonelConfiguration : IEntityTypeConfiguration<Personel>
    {
        public void Configure(EntityTypeBuilder<Personel> builder)
        {
            builder.ToTable("Personeller");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Ad)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(p => p.Rutbe);
            builder.Property(p => p.PazarSayisi).HasDefaultValue(0);
            builder.Property(p => p.OzelGunSayisi).HasDefaultValue(0);
            builder.Property(p => p.HaftaIciSayisi).HasDefaultValue(0);
            builder.Property(p => p.YedekSayisi).HasDefaultValue(0);

            builder.HasMany(p => p.Etutler)
                   .WithOne(e => e.Personel)
                   .HasForeignKey(e => e.PersonelId);

            builder.HasMany(p => p.Mazeretler)
                   .WithOne()
                   .HasForeignKey(m => m.PersonelId);
        }
    }
}