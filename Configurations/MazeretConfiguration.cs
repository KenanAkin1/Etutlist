using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etutlist.Models;

namespace Etutlist.Configurations
{
    public class MazeretConfiguration : IEntityTypeConfiguration<Mazeret>
    {
        public void Configure(EntityTypeBuilder<Mazeret> builder)
        {
            builder.ToTable("Mazeretler");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Baslangic).IsRequired();
            builder.Property(m => m.Bitis).IsRequired();

            builder.HasOne(m => m.Personel)
                   .WithMany(p => p.Mazeretler)
                   .HasForeignKey(m => m.PersonelId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}