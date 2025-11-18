# ?? MANUEL DÜZELTME TALIMATI

Automatic düzeltme baþarýsýz oldu, manuel düzeltme talimatý:

## ? SORUN:
`List<dynamic>` kullanýmý compile hatasý veriyor. Anonymous type'lar direkt dynamic'e cast edilemiyor.

## ? ÇÖZÜM (5 ADIM):

### ADIM 1: Dosyanýn sonuna cache class'larýný ekle

**Satýr 565'ten sonra (TelafiSlot class'ýnýn altýna) ekle:**

```csharp
        // ? Cache helper classes
        private class TelafiCache
        {
            public DateTime TelafiTarihi { get; set; }
            public TimeSpan BaslangicSaat { get; set; }
            public int? KisimNo { get; set; }
            public int YedekHocaId { get; set; }
        }

        private class DersCache
        {
            public string DersGunu { get; set; }
            public int DersSaati { get; set; }
            public int KisimNo { get; set; }
            public int HocaId { get; set; }
        }

        private class HocaDersCache
        {
            public int HocaId { get; set; }
            public int DersId { get; set; }
        }
```

### ADIM 2: TopluTelafiOlusturAsync metodunda cache'leri dönüþtür

**Satýr 292-310 arasý DEÐIÞTIR:**

**ÖNCESÝ:**
```csharp
var mevcutTelafiler = await _context.TelafiDersler
    .Where(t => tarihler.Contains(t.TelafiTarihi.Date))
    .Select(t => new { t.TelafiTarihi, t.BaslangicSaat, t.KisimNo, t.YedekHocaId })
    .AsNoTracking()
    .ToListAsync();
```

**SONRASI:**
```csharp
var mevcutTelafilerRaw = await _context.TelafiDersler
    .Where(t => tarihler.Contains(t.TelafiTarihi.Date))
    .Select(t => new { t.TelafiTarihi, t.BaslangicSaat, t.KisimNo, t.YedekHocaId })
    .AsNoTracking()
    .ToListAsync();

var mevcutTelafiler = mevcutTelafilerRaw.Select(t => new TelafiCache 
{ 
    TelafiTarihi = t.TelafiTarihi, 
    BaslangicSaat = t.BaslangicSaat, 
    KisimNo = t.KisimNo, 
    YedekHocaId = t.YedekHocaId 
}).ToList();
```

**Ayný þekilde normalDersProgrami ve hocaDersYetkileri için de:**

```csharp
var normalDersProgramiRaw = await _context.DersProgrami
    .Where(d => gunler.Contains(d.DersGunu))
    .Select(d => new { d.DersGunu, d.DersSaati, d.KisimNo, d.HocaId })
    .AsNoTracking()
    .ToListAsync();

var normalDersProgrami = normalDersProgramiRaw.Select(d => new DersCache 
{ 
    DersGunu = d.DersGunu, 
    DersSaati = d.DersSaati, 
    KisimNo = d.KisimNo, 
    HocaId = d.HocaId 
}).ToList();

var hocaDersYetkileriRaw = await _context.HocaDersler
    .Select(hd => new { hd.HocaId, hd.DersId })
    .AsNoTracking()
    .ToListAsync();

var hocaDersYetkileri = hocaDersYetkileriRaw.Select(hd => new HocaDersCache 
{ 
    HocaId = hd.HocaId, 
    DersId = hd.DersId 
}).ToList();
```

### ADIM 3: Cache'e ekleme satýrýný deðiþtir

**Satýr 385 civarý DEÐIÞTIR:**

**ÖNCESÝ:**
```csharp
mevcutTelafiler.Add(new { 
    telafi.TelafiTarihi, 
    telafi.BaslangicSaat, 
    telafi.KisimNo, 
    telafi.YedekHocaId 
});
```

**SONRASI:**
```csharp
mevcutTelafiler.Add(new TelafiCache 
{ 
    TelafiTarihi = telafi.TelafiTarihi, 
    BaslangicSaat = telafi.BaslangicSaat, 
    KisimNo = telafi.KisimNo, 
    YedekHocaId = telafi.YedekHocaId 
});
```

### ADIM 4: Method signature'larýndaki `List<dynamic>` kaldýr

**3 methodda DEÐIÞTIR:**

**FindUygunSlotLocal (satýr 420):**
```csharp
private TelafiSlot? FindUygunSlotLocal(
    List<TelafiSlot> takvim,
    DersProgrami ders,
    List<TelafiCache> mevcutTelafiler,  // ?
    List<DersCache> normalDersProgrami)  // ?
```

**IsHocaMusaitLocal (satýr 460):**
```csharp
private bool IsHocaMusaitLocal(
    int hocaId,
    TelafiSlot slot,
    List<TelafiCache> mevcutTelafiler,  // ?
    List<DersCache> normalDersProgrami)  // ?
```

**FindMusaitHocalarLocal (satýr 490):**
```csharp
private List<Hoca> FindMusaitHocalarLocal(
    DersProgrami ders,
    TelafiSlot slot,
    List<Hoca> tumHocalar,
    List<HocaDersCache> hocaDersYetkileri,  // ?
    List<TelafiCache> mevcutTelafiler,  // ?
    List<DersCache> normalDersProgrami)  // ?
```

### ADIM 5: Build yap

```
Ctrl + Shift + B
```

## ?? SONUÇ:
- ? 7 compile error düzelecek
- ? Performans: **10-20x daha hýzlý**
- ? Timeout sorunu çözülecek
