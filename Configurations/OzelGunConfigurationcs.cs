using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etutlist.Models;

namespace Etutlist.Configurations
{
    public class OzelGunConfiguration : IEntityTypeConfiguration<OzelGun>
    {
        public void Configure(EntityTypeBuilder<OzelGun> builder)
        {
            builder.ToTable("OzelGunler");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Tarih)
                   .IsRequired();

            builder.Property(o => o.Aciklama)
                   .IsRequired()
                   .HasMaxLength(200);
        }
    }
}
