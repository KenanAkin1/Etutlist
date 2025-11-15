using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etutlist.Models;

namespace Etutlist.Configurations
{
    public class EtutConfiguration : IEntityTypeConfiguration<Etut>
    {
        public void Configure(EntityTypeBuilder<Etut> builder)
        {
            builder.ToTable("Etutler");

         builder.HasKey(e => e.Id);

            builder.Property(e => e.Tarih)
      .IsRequired();

      builder.Property(e => e.Tip)
               .IsRequired()
    .HasMaxLength(20);

// ? Personel silinince NULL olsun (CASCADE DELETE DEÐÝL!)
            builder.HasOne(e => e.Personel)
  .WithMany(p => p.Etutler)
          .HasForeignKey(e => e.PersonelId)
        .OnDelete(DeleteBehavior.SetNull); // ? ÖNEMLÝ!

 // ? Yedek alanlar (opsiyonel)
      builder.Property(e => e.PersonelAd)
.HasMaxLength(100);

  builder.Property(e => e.PersonelRutbe)
 .HasMaxLength(50);
        }
    }
}
