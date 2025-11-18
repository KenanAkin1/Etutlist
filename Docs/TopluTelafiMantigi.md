# TOPLU TELAFÝ SÝSTEMÝ - DOÐRU MANTIK

## ÖNEMLÝ: Mantýk Deðiþikliði

### ESKÝ (YANLIÞ):
- TelafiYapilamayacakGun ? Telafi yapýlamayacak gün
- Toplu telafide bu günde telafi yapýlacak

### YENÝ (DOÐRU):
- TelafiYapilacakGun ? Telafi yapýlabilecek gün  
- Toplu telafide baþka bir günün dersleri bu güne atanacak

## ÖRNEK SENARYO:

### 1. Telafi Ayarlarý (ASEM 1. Tabur):
```
Pazartesi: Telafi yapýlabilir (7-9. saatler)
Salý: Telafi yapýlabilir (8-9. saatler)
Çarþamba: Telafi yapýlamaz
Perþembe: Telafi yapýlabilir (7-9. saatler)
Cuma: Telafi yapýlamaz
```

### 2. Toplu Telafi Ýsteði:
```
Tarih: 17.11.2025 (Salý)
Durum: Bu Salý günü tatil, dersleri telafi edilecek
```

### 3. Sistem Ýþlemi:
```
1. Salý günü derslerini bul (25 ders)
2. Her ders için telafi takviminden uygun günleri bul:
   - Pazartesi 7-9
   - Salý 8-9 (HAYIR - ayný gün olamaz)
   - Perþembe 7-9
3. Bu günlerdeki boþ slotlara ders ata
4. Çakýþma kontrolü yap
```

## SÝSTEM AKIÞI:

```
Normal Ders: Salý 1. ders - Fizik - Kýsým 5
                    ?
         (Bu Salý tatil, telafi edilecek)
                    ?
    Telafi ayarlarýný kontrol et:
    - Pazartesi: ? 7-9 uygun
    - Perþembe: ? 7-9 uygun
                    ?
    Önümüzdeki Pazartesi/Perþembe günlerinde boþ slot bul:
    - 18.11.2025 Pazartesi 7. ders: ? Boþ
                    ?
         Telafi oluþtur:
    Tarih: 18.11.2025 Pazartesi
    Saat: 7. ders (14:00-14:40)
    Ders: Fizik
    Kýsým: 5
    Neden: 17.11.2025 Salý tatil nedeniyle
```

## KRÝTÝK NOKTALAR:

1. **TelafiYapilacakGun:** Bu günde telafi dersleri yapýlabilir
2. **Tarih seçimi:** Hangi günün dersleri telafi edilecek
3. **Takvim oluþturma:** Önümüzdeki 30 gün için telafi yapýlabilir günleri bul
4. **Çakýþma kontrol:** Ayný kýsým/hoca/saat çakýþmasý
5. **Slot atama:** Boþ bulduðu ilk uygun slota ata
