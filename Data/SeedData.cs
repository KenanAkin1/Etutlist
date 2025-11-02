using Etutlist.Models;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            // Veritabanını temizle (sadece test ortamında güvenli)
            if (context.Mazeretler.Any())
            {
                context.Mazeretler.RemoveRange(context.Mazeretler);
            }
            if (context.OzelGunler.Any())
            {
                context.OzelGunler.RemoveRange(context.OzelGunler);
            }
            if (context.Personeller.Any())
            {
                context.Personeller.RemoveRange(context.Personeller);
            }
            context.SaveChanges();

            // Personeller
            var personeller = new List<Personel>
            {
                new Personel { Ad = "Ahmet Yılmaz" },
                new Personel { Ad = "Ayşe Demir" },
                new Personel { Ad = "Mehmet Kaya" },
                new Personel { Ad = "Zeynep Çelik" },
                new Personel { Ad = "Ali Şahin" },
                new Personel { Ad = "Fatma Koç" },
                new Personel { Ad = "Mustafa Arslan" },
                new Personel { Ad = "Elif Yıldız" }
                // ... 80 kişiye kadar ekleyebilirsin
            };

            context.Personeller.AddRange(personeller);
            context.SaveChanges();

            // Özel günler
            context.OzelGunler.AddRange(
                new OzelGun { Tarih = new DateTime(2025, 10, 29), Aciklama = "Cumhuriyet Bayramı" },
                new OzelGun { Tarih = new DateTime(2025, 11, 10), Aciklama = "Atatürk'ü Anma Günü" }
            );
            context.SaveChanges();

            // Mazeretler (navigation property kullanarak)
            var ahmet = context.Personeller.First(p => p.Ad == "Ahmet Yılmaz");
            var ayse = context.Personeller.First(p => p.Ad == "Ayşe Demir");

            context.Mazeretler.AddRange(
                new Mazeret { Personel = ahmet, Baslangic = new DateTime(2025, 10, 20), Bitis = new DateTime(2025, 10, 22) },
                new Mazeret { Personel = ayse, Baslangic = new DateTime(2025, 10, 25), Bitis = new DateTime(2025, 10, 25) }
            );
            context.SaveChanges();
        }
    }
}