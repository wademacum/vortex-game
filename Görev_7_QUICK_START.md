# Görev 7 Hızlı Başlangıç Rehberi

## 30 Saniyelik Test

```
1. Play'e basın (▶)
2. GeodesicOrbitTestRunner component'ini seçin (Hierarchy'de ara)
3. Inspector'da sağ tık → Context Menu
   - "Test/Apply Low Speed Scenario" seçin
4. 10 saniye bekleyin, yörüngeyi izleyin
5. Play'i durdurun (⏸)

SONUÇ: Kapalı dairesel yörünge görünmeli = ✅ TEST PASS
```

---

## 3 Dakikalık Tam Test

### Test 1: Newton Uyumluluğu
```
GeodesicOrbitTestRunner → Test/Apply Low Speed Scenario
⏱️ 10 saniye izle → Kapalı daire mi? ✅ PASS
```

### Test 2: Schwarzschild Öncesyonu
```
GeodesicOrbitTestRunner → Test/Apply High Speed Scenario
⏱️ 10 saniye izle → Spiral/presesyon var mı? ✅ PASS
```

### Test 3: Zaman Dondurma
```
GeodesicOrbitTestRunner → Test/Run Freeze ProperTime Check
⏱️ 3 saniye auto → Konsola "Freeze PASS" yazılıyor mu? ✅ PASS
```

### Test 4: Stres (50 Cisim)
```
GeodesicTestHarness → Test/Spawn Stress Bodies
⏱️ 30 saniye → 50 cisim hareket ediyor mu? FPS 60+ mı?
Profiler: Window → Analysis → Profiler → Bak
✅ PASS
```

---

## 🎬 Screenshot Komutu (Otomatik)

Play mode'da, sahnedeki **kameraya** bakılıyor. Aşağıdaki script çalıştırırsanız screenshot'ı alır:

```csharp
using UnityEngine;

public class TestScreenshot : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            string filename = $"test_orbit_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log($"Screenshot saved: {filename}");
        }
    }
}
```

---

## 📋 Checklist

- [ ] Test sahne açılıyor: `Assets/Scenes/Tests/GeodesicOrbitTest.unity`
- [ ] Play mode'da GravityWell görünüyor (Gizmo = kırmızı küre)
- [ ] Yörünge çiziliyor (LineRenderer görünüyor)
- [ ] Konsol: NaN/Infinity yok
- [ ] FPS meter gösteriliyor (Play mod'da sağ üst)

---

## 🔗 Sahnede Hangi Nesneler Var?

`Assets/Scenes/Tests/GeodesicOrbitTest.unity` içerisinde:

1. **GravityWell** (GameObject)
   - Component: `GravityWell.cs` (mass = 1.0)
   
2. **TestOrbitBody** (GameObject)
   - Component: `RelativisticBody.cs`
   - Component: `OrbitVisualizer.cs` (LineRenderer)
   
3. **Manager/Systems**
   - `GeodesicSystem` (IJobParallelFor scheduler)
   - `GravityWellRegistry` (singleton pattern)
   
4. **Test Controllers**
   - `GeodesicTestHarness` (setup harness)
   - `GeodesicOrbitTestRunner` (scenario runner)

---

## ⚠️ Eğer Hata Alırsan

| Hata | Çözüm |
|------|-------|
| "Method not found" | Compile hatası var. Ctrl+Shift+R (Force Recompile) |
| Yörünge çizilmiyor | LineRenderer eklenmiş mi kontrol et |
| NaN konsola yazılıyor | RelativisticBody.ProperTime = 1.0 (dondurma kaldır) |
| 60 FPS'ten düşük | 50 cisim → 25 cisim test et |
| GravityWell görünmüyor | Scene view'de Gizmos açık mı? (Sağ üst toggle) |

---

## ✅ FAZ 2'ye Geçiş İçin Gerekli

Tüm test senaryoları PASS olmalı:
- [x] Setup tamamlandı (script + documentation)
- [ ] Low-speed PASS (Newton uyumluluğu)
- [ ] High-speed PASS (Schwarzschild öncesyonu)
- [ ] Freeze PASS (ProperTime dondurma)
- [ ] Stress PASS (50 cisim @ 60 FPS)
- [ ] Console temiz (NaN/Infinity yok)

---

**Sonraki Adım:** Lokal makinede Play mode'da testleri çalıştırmak
