# Görev 7 - Test Çalıştırma Rehberi (Manual)

## ✅ Hazırlık Tamamlandı

- Sahne: `Assets/Scenes/Tests/GeodesicOrbitTest.unity`  
- Setup Script: `Assets/Scripts/Debug/TestSetup_Görev7.cs`
- Test Runners: `GeodesicTestHarness` + `GeodesicOrbitTestRunner`
- Docs: Detaylı test spesifikasyonu ve kontrol listeleri oluşturuldu

---

## 🎬 Test Çalıştırma (Play Mode)

### Adım 1: Sahne Aç

```
1. Unity Editor'de: File → Open Scene
2. Seç: Assets/Scenes/Tests/GeodesicOrbitTest.unity
3. Enter veya double-click
```

### Adım 2: Play'e Bas

```
Toolbar'da: Play (▶) tuşu
veya
Keyboard: Ctrl + P
```

### Adım 3: Test Senaryolarını Sırayla Çalıştır

---

## 🧪 Senaryo 1: Düşük Hız (Newton Uyumluluğu) - 10 dakika

```
1. Hierarchy'de "GeodesicOrbitTestRunner" arayın
2. Seçin (click)
3. Inspector'da sağ tık → "Test/Apply Low Speed Scenario"
4. 10 saniye izleyin
```

**Beklenen Sonuç:**
- ✅ LineRenderer'da **kapalı dairesel yörünge** çiziliyor
- ✅ Konsol: `[GeodesicOrbitTestRunner] Low-speed scenario applied.` yazılı
- ✅ NaN/Infinity yok
- ✅ FPS > 60

**Hata Durumunda:**
- Yörünge kapatılmıyor → geodezik integratör problem var
- NaN yazmışsa → sayı taşması, başlangıç hız problemi
- FPS düşükse → Job System Burst derlemesi kontrol et

---

## 🧪 Senaryo 2: Yüksek Hız (Schwarzschild Öncesyonu) - 10 dakika

```
1. GeodesicOrbitTestRunner seçin
2. Inspector → sağ tık → "Test/Apply High Speed Scenario"
3. 20 saniye izleyin
```

**Beklenen Sonuç:**
- ✅ Yörünge **spiral/presesyon** hareketli
- ✅ Konsol: `[GeodesicOrbitTestRunner] High-speed scenario applied.`
- ✅ Yörünge stabilitesi korunuyor (çökmüyor)
- ✅ 60 FPS

**Başarı İşareti:**
- Ellips eksenleri yavaşça rotasyon yapıyor = Schwarzschild öncesyonu görülüyor! ✓

**Hata Durumunda:**
- Presesyon yoksa → `localDeltaTime` hesaplamacalculation problemi
- Kapatılmasa → yüksek hızda enerji kaybı

---

## 🧪 Senaryo 3: ProperTime Freeze - 5 dakika

```
1. GeodesicOrbitTestRunner seçin
2. Inspector → sağ tık → "Test/Run Freeze ProperTime Check"
3. Otomatik 3 saniye sonra sonuç gösterilir
```

**Beklenen Sonuç:**
- ✅ Konsol: `[GeodesicOrbitTestRunner] Freeze PASS. Drift=0.00000`
- ✅ Cisim 2 saniye boyunca hiç hareket etmez
- ✅ Sonrasında hızlanır (restore)

**Hata Durumunda:**
- `Freeze FAIL` yazılırsa → drift toleransi aşıldı, harita hatasıgüncelleme sorunu
- Drift > 0.05 → ProperTime dondurma logic'i sorun

---

## 🧪 Senaryo 4: Stres Testi (50 Cisim) - 15 dakika

```
1. Hierarchy'de "GeodesicTestHarness" arayın
2. Seçin
3. Inspector → sağ tık → "Test/Spawn Stress Bodies"
4. 30 saniye izleyin
```

**Beklenen Sonuç:**
- ✅ 50 tane küçük küre spawanıyor
- ✅ Hepsi dairesel yörüngelerde hareket ediyor
- ✅ **Profiler: 60 FPS üzeri**

**Performance Kontrol:**
```
1. Window → Analysis → Profiler
2. Frame Rate panel → Max FPS kontrol et
3. CPU Usage → GeodesicSystem Job sistem yüksek mi? (normal)
4. Memory → Stabil mı?
```

**Başarı:**
- 60 FPS stabil = ✅ RTX 3050 hedef sağlanıyor

**Hata Durumunda:**
- FPS < 50 → Job System optimization gerekli
- NaN yazılırsa → sayı overflow, hız clamp'ı check et

---

## 📊 Test Sonucu Rapor Formatı

Tüm testler PASS ise aşağıdaki raporu doldurunuz:

```
GÖREV 7 TEST RAPOR - 2026-04-25

✅ TEST 1: NEWTON UYUMLULUĞU (Low-Speed)
   - Yörünge tipi: Kapalı Daire ✓
   - FPS: > 60 ✓
   - NaN/Infinity: Yok ✓

✅ TEST 2: SCHWARZSCHILD ÖNCESYONu (High-Speed)
   - Yörünge tipi: Presesyon Spiral ✓
   - Stabilite: Korunuyor ✓
   - NaN/Infinity: Yok ✓

✅ TEST 3: PROPERTIME FREEZE
   - Freeze PASS: Evet ✓
   - Drift: < 0.05 ✓
   - Restore: Çalışıyor ✓

✅ TEST 4: STRES (50 Cisim)
   - Cisim spawned: 50 ✓
   - FPS: > 60 ✓
   - Crash/NaN: Yok ✓

SONUÇ: ✅ FAZ 2'ye PASS - Tüm görevler geçti!
```

---

## 🐛 Troubleshooting

| Sorun | Çözüm |
|-------|-------|
| Play mode'da sahne açılmıyor | Sahne path kontrol et, .unity dosyası var mı? |
| GeodesicTestHarness/Runner bulunamıyor | Hierarchy'de Search (Ctrl+F) yap |
| Yörünge çizilmiyor | LineRenderer component aktif mi kontrol et |
| Stres bodi spawnmıyor | GravityWell sahneye koyulmuş mu kontrol et |
| NaN konsola yazılıyor | RelativisticBody.ProperTime dondurma hatası |
| Compile error | Ctrl+Shift+R (Force Recompile) çalıştır |
| 30 FPS'ten düşük | Stres cisim sayısı azalt (50→25) |

---

## 📝 Notlar

- **Test süresi:** ~40 dakika (tüm senaryolar + kontrol)
- **Gerekli:** RTX 3050 veya benzer (60 FPS target)
- **Kamera:** Scene View'de izleyin veya Game View (F)
- **Screenshot:** Play mod'da F12 (ekle Custom handler)

---

## ✅ FAZ 2 Kapısı Onayı

Tüm testler PASS ise = FAZ 2 başlangıç kriteri sağlandı

```
FAZ 1 ✓ TAMAMLANDI
├─ Görev 1-6: Fizik altyapısı ✓
└─ Görev 7: Test doğrulanmış ✓

FAZ 2 🎮 BAŞLANACAK
├─ Vortex Arazı (Compute Shader)
├─ SDF Marching Cubes
└─ Gezegen görselleştirmesi
```

---

**Başla:** Play (▶) → Seçim → Context Menu → Test Senaryosu → İzle → Sonuç Kontrol
