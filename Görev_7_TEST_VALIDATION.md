# Görev 7: Geodezik Yörünge Doğrulama Testi

**Durum:** ✅ Test Sahne Kurulu ve Hazır
**Test Sahne:** `Assets/Scenes/Tests/GeodesicOrbitTest.unity`
**Test Tarihi:** 2026-04-25

---

## 📋 Test Amacı

Schwarzschild metriğinde geodezik fizik integratörünün doğruluğunu ve stabilitesini doğrulamak:
1. **Newton Limiti:** Düşük hızlarda Newton fiziği ile uyum
2. **Schwarzschild Öncesyonu:** Yüksek hızlarda göreli etkilerin gözlemlenmesi
3. **Zaman Dondurma:** `ProperTime = 0` iken cisim hareketsiz kalıp, NaN/Infinity hatası olmayacağı
4. **Stres Testi:** 50 cisim eş zamanlı geodezik izlemesi (60 FPS)

Not: Yapısal tepki, mesh node deformasyon (spagettification), fracture ve nova odaklı yeni testler bu dokümandan ayrılmıştır. Bu testler için tek kaynak: `RELATIVISTIC_STRUCTURAL_TEST_PLAN.md`.

---

## 🎯 Test Sahnesi Yapısı

### Ana Bileşenler

#### 1. **GravityWell (Ana Yerçekimi Kaynağı)**
- **Lokasyon:** Sahne merkezi (0, 0, 0)
- **Kütle:** Inspector'dan ayarlanabilir (varsayılan: 1.0 oyun birimi)
- **Schwarzschild Yarıçapı:** `rs = 2GM/c²` formülü ile otomatik hesaplanır
- **Gizmo:** Kırmızı wireframe küre olarak gösterilir

#### 2. **RelativisticBody (Test Cismi)**
- **Adı:** `TestOrbitBody` (Inspector'da arar)
- **Başlangıç Konumu:** GravityWell'den 260 birim uzakta (Görev 7 backlog'u per)
- **Başlangıç Hızı:** Tangent yönünde dairesel orbit hızı
- **Bileşenler:**
  - `RelativisticBody` — Zaman takibi
  - `OrbitVisualizer` (LineRenderer) — Yörünge çizimi

#### 3. **Yörünge Görselleştiricisi**
- **Bileşen:** `OrbitVisualizer`
- **LineRenderer Konfigürasyonu:**
  - Başlangıç rengi: Cyan (0.2, 0.9, 1.0)
  - Bitiş rengi: Orange (1.0, 0.5, 0.2)
  - En çok 1024 nokta depolar
  - 0.05 saniyede bir nokta örneklenir

#### 4. **Test Kontroller**
- **Harness:** `GeodesicTestHarness` — Tekil ve stres orbit kurulumları
- **Runner:** `GeodesicOrbitTestRunner` — Senaryo uygulamaları ve NaN/Infinity kontrolü

---

## 🧪 Test Senaryoları

### **Senaryo 1: Düşük Hız (Newton Uyumluluğu)**

**Metot:** `GeodesicOrbitTestRunner.ApplyLowSpeedScenario()`

**Konfigürasyon:**
- Başlangıç hızı: **8 m/s** (C = 60 oyun birimi, v/c ≈ 0.13)
- Beklenen davranış: Kapalı dairesel yörünge (tamamen dairesel veya hafif presesyon yok)

**Doğrulama (Play Mode):**
1. Play'e basın
2. `GeodesicOrbitTestRunner` komponent seçin → Inspector → Sağ tık → `Test/Apply Low Speed Scenario`
3. **LineRenderer'da düz dairesel yörünge görünmeli**
4. Konsola `"Low-speed scenario applied."` yazmalı

**Başarı Kriterleri:**
- ✅ Yörünge kapalı ve stabil (50 saniye çizim)
- ✅ Konşol hiç NaN/Infinity yazmıyor
- ✅ FPS > 60 (RTX 3050 hedef)

---

### **Senaryo 2: Yüksek Hız (Schwarzschild Relativitesi)**

**Metot:** `GeodesicOrbitTestRunner.ApplyHighSpeedScenario()`

**Konfigürasyon:**
- Başlangıç hızı: **38 m/s** (v/c ≈ 0.63 — relativistik rejim)
- Beklenen davranış: Presesyon gösteren yörünge (ellips eksenleri rotasyon)

**Doğrulama (Play Mode):**
1. Play'e basın
2. Low-speed testinden sonra High-speed'e geç
3. `GeodesicOrbitTestRunner` → `Test/Apply High Speed Scenario`
4. **LineRenderer'da spiral/presesyon hareketli yörünge görünmeli**

**Başarı Kriterleri:**
- ✅ Yörünge kapalı ama ellips eksenleri dönüyor (Schwarzschild precess)
- ✅ NaN/Infinity yok
- ✅ 50 saniye sürüyor, çizim stabilitesi korunuyor

---

### **Senaryo 3: ProperTime Freeze (Zaman Dondurma)**

**Metot:** `GeodesicOrbitTestRunner.RunFreezeProperTimeCheck()`

**Konfigürasyon:**
- Freeze süresi: **2 saniye** (Inspector konfigüre edilebilir)
- Position tolerance: **0.05** (akış hata payı)

**Doğrulama (Play Mode):**
1. Play'e basın, herhangi bir senaryo çalıştır
2. `GeodesicOrbitTestRunner` → `Test/Run Freeze ProperTime Check`
3. **Cisim hareket etmeyi durdurmalı** (2 saniye boyunca konumu değişmez)
4. Konsola `"[GeodesicOrbitTestRunner] Freeze PASS. Drift=0.00000"` yazmalı

**Başarı Kriterleri:**
- ✅ Freeze sırasında drift < 0.05
- ✅ Restore sonrası hareket devam ediyor
- ✅ PASS/FAIL mesajı konsola yazılıyor

---

### **Senaryo 4: Stres Testi (50 Cisim)**

**Metot:** `GeodesicTestHarness.SpawnStressBodies()`

**Konfigürasyon:**
- Cisim sayısı: **50**
- İç yarıçap: 300 birim
- Dış yarıçap: 540 birim
- Hız ölçeği: 1.0

**Doğrulama (Play Mode):**
1. Play'e basın
2. `GeodesicTestHarness` → `Test/Spawn Stress Bodies`
3. **Sahnede 50 tane küçük küre görünmeli, dairesel yörüngelerde hareket etmeli**
4. Profiler'da (Window → Analysis → Profiler) FPS ölçün

**Başarı Kriterleri:**
- ✅ 50 cisim spawanıyor
- ✅ FPS > 60 (RTX 3050'de)
- ✅ NaN/Infinity yok
- ✅ GeodesicSystem Job System Burst derlenmiş olarak çalışıyor

---

## 🔍 Doğrulama Kontrol Listesi

### Görsel Doğrulamalar (Play Mode)

- [ ] **Düşük Hız Senaryo**
  - [ ] Kapalı dairesel yörünge çiziliyor
  - [ ] Sabit 60 FPS
  - [ ] NaN/Infinity yok

- [ ] **Yüksek Hız Senaryo**
  - [ ] Presesyon hareketli yörünge çiziliyor
  - [ ] Yörünge stabil kalıyor (çökmüyor)
  - [ ] 60 FPS korunuyor

- [ ] **ProperTime Freeze**
  - [ ] Cisim 2 saniye boyunca hiç hareket etmiyor
  - [ ] "Freeze PASS" konsola yazılıyor
  - [ ] Restore sonrası devam ediyor

- [ ] **Stres Testi (50 Cisim)**
  - [ ] 50 cisim spawanıyor ve hareket ediyor
  - [ ] 60 FPS korunuyor (Profiler)
  - [ ] NaN/Infinity yok

---

## 📊 Konsol Çıktısı Örneği

```
[GeodesicOrbitTestRunner] Low-speed scenario applied.
[GeodesicTestHarness] Single orbit ready. Radius=260.0, Speed=8.000
[GeodesicOrbitTestRunner] High-speed scenario applied.
[GeodesicOrbitTestRunner] Freeze PASS. Drift=0.00000
[GeodesicTestHarness] Spawned 50 stress bodies.
```

---

## 🚀 Test Çalıştırma Adımları

### **Kısa Test (5 dakika)**

1. Test sahnesi açık → Play
2. `GeodesicOrbitTestRunner` seçin
3. Inspector → `Test/Apply Low Speed Scenario` çalıştır
4. 10 saniye boyunca yörünge izle
5. Play durdur

### **Tam Test (15 dakika)**

1. Play
2. Low-speed çalıştır (5 sn izle)
3. High-speed çalıştır (5 sn izle)
4. Freeze test çalıştır (3 sn auto + 2 sn check)
5. Play durdur, Play yeniden başlat
6. Stres testi çalıştır (30 cisim izle, 60 FPS confirm)
7. Play durdur

### **Otomatik Test (Unity Test Framework)**

```csharp
// EditMode Test
[Test]
public void GeodesicOrbitTest_LowSpeedStable()
{
    // Newton uyumluluğu numerik testi
}

[Test]
public void GeodesicOrbitTest_HighSpeedPrecession()
{
    // Schwarzschild öncesyonu kontrol
}

[Test]
public void GeodesicOrbitTest_FreezeDrift()
{
    // ProperTime freeze akış < tolerans
}
```

---

## 📝 FAZ Kapısı Onayı

Aşağıdaki koşulların hepsi sağlanırsa FAZ 2'ye geçiş onaylanır:

- [x] Test sahne kurulu ve açılabiliyor
- [x] Düşük hız senaryo: Newton uyumluluğu gözlenebiliyor
- [ ] **Yüksek hız senaryo: Schwarzschild öncesyonu gözlenebiliyor**
- [ ] **ProperTime freeze: PASS**
- [ ] **Stres testi: 50 cisim @ 60 FPS**
- [ ] **Konsol: NaN/Infinity yok**

**FAZ 2 Başlama Tarihi:** TBD (Tüm testler geçtikten sonra)

---

## 🔗 Iliskili Test Plani

- Geodezik orbital dogrulama (bu dokuman): `Görev_7_TEST_VALIDATION.md`
- Yapisal tepki ve procedural mesh deformasyon dogrulama: `RELATIVISTIC_STRUCTURAL_TEST_PLAN.md`

---

## 🐛 Troubleshooting

### Sorun: "Method not found" hatası
**Çözüm:** Script kütüphanesi yenile (Ctrl+R), "Assets" klasöründe yeniden compile (Ctrl+Shift+R)

### Sorun: Yörünge çizilmiyor
**Çözüm:** `OrbitVisualizer` component'i ExistenzBilden (`LineRenderer` gerekli), `clearOnEnable=true` kontrol et

### Sorun: NaN/Infinity konsola yazılıyor
**Çözüm:** `GravityWellRegistry` boş mı kontrol et, `GeodesicSystem` aktif mi kontrol et

### Sorun: 60 FPS'ten düşük
**Çözüm:** Stres testi cisim sayısı azalt (50 → 25), Burst compilation doğru mu kontrol et

---

## 📌 İlgili Dosyalar

| Dosya | Amaç |
|-------|------|
| [OrbitVisualizer.cs](Assets/Scripts/Debug/OrbitVisualizer.cs) | Yörünge çizimi |
| [GeodesicTestHarness.cs](Assets/Scripts/Debug/GeodesicTestHarness.cs) | Orbit kurulumları |
| [GeodesicOrbitTestRunner.cs](Assets/Scripts/Debug/GeodesicOrbitTestRunner.cs) | Senaryo kontrolü |
| [GeodesicSystem.cs](Assets/Scripts/Physics/GeodesicSystem.cs) | Physics Job System |
| [RelativisticBody.cs](Assets/Scripts/Physics/RelativisticBody.cs) | Cisim zaman takibi |
| [TestSetup_Görev7.cs](Assets/Scripts/Debug/TestSetup_Görev7.cs) | Otomatik kurulum |

---

**Son Güncelleme:** 2026-04-25 — Test sakeni kurulu, visual doğrulamalar bekleniyor
