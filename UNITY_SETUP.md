# Dosya: unity_setup.md
# VORTEX EVRENİ — UNITY PROJE KURULUM KONTROL LİSTESİ

> **Amaç:** Unity açılmadan önce ve açıldıktan hemen sonra yapılması gereken tüm ayarları tek belgede toplamak. Bu belgede yazılı bir ayar atlanırsa render, Burst derleme veya NaN hataları oluşabilir ve sebebi bulmak saatler alabilir.
> (Bkz: `Architecture.md` 1 ve `BACKLOG.md` FAZ 1 Görev 1)

---

## 1. UNITY HUB: PROJE OLUŞTURMA

- [ ] Unity **6.3 LTS (6000.3.14f1)** sürümü (non-LTS kullanılmaz; ekipte tek patch versiyonuna kilitlenir)
- [ ] Şablon: **High Definition RP** (3D Core değil — HDRP şablonu seçilmeli)
- [ ] Proje adı: `Vortex`
- [ ] Konum: Boşluksuz bir dizin yolu (Unity boşluklu yollarda Burst derleme hataları verebilir)
- [ ] Oluşturulduktan sonra Git repo başlat; `.gitignore` Unity şablonunu kullan

---

## 2. PROJE AYARLARI (Edit → Project Settings)

### 2.1. Player
| Ayar | Değer | Neden |
|---|---|---|
| **Color Space** | **Linear** | HDRP Linear beklentisi; sonradan değiştirmek tüm materyalleri bozar |
| **Scripting Backend** | IL2CPP | Burst AOT derleme için gerekli (release build'de) |
| **Api Compatibility Level** | .NET Standard 2.1 | Unity.Mathematics ve Collections uyumluluğu |
| **Allow Unsafe Code** | ✅ Açık | NativeArray işaretçi operasyonları için gerekli |

### 2.2. Physics
| Ayar | Değer | Neden |
|---|---|---|
| **Gravity** | (0, 0, 0) | Newton yerçekimi YASAK — PRD.md 1 |
| **Auto Simulation** | ❌ Kapalı | Fizik döngüsü GeodesicSystem tarafından manuel yönetilir |
| **Gravity Scale** | 0 | Ek güvenlik katmanı |

> **Kritik:** `Physics.autoSimulation = false` sadece Project Settings'te değil, `GeodesicSystem.Awake()` içinde de kod ile set edilmeli. Sahne yükleme sırasında sıfırlanabilir.

### 2.3. Time
| Ayar | Değer | Neden |
|---|---|---|
| **Fixed Timestep** | 0.02 (50 Hz) | RK4 adımı için yeterli; 0.01'e düşürülebilir gerekirse |
| **Maximum Allowed Timestep** | 0.1 | Büyük lag spike'larında NaN riskini azaltır |

### 2.4. Quality
- Başlangıçta tek kalite seviyesi bırak: **High**
- Diğer seviyeler kaldırılabilir (geliştirme aşamasında kafa karışıklığı önler)

### 2.5. Input System
- **Active Input Handling:** **Input System Package (New)** — `ShipController` ve `TimeWeapon` yeni Input System kullanır
- Eski Input Manager tamamen devre dışı bırakılır

---

## 3. HDRP ASSET AYARLARI

> HDRP Asset: `Assets/Settings/HDRenderPipelineAsset.asset` (şablon ile gelir)

### 3.1. Rendering
| Ayar | Değer |
|---|---|
| Lit Shader Mode | **Deferred** (çok ışık kaynağı için daha verimli) |
| Motion Vectors | ✅ (TAA için gerekli) |
| Runtime Debug Display | ❌ Kapalı (performans) |

### 3.2. Shadows (RTX 3050 limitine göre)
| Ayar | Değer |
|---|---|
| Max Shadow Distance | 500 |
| Directional Shadow Resolution | **2048** |
| Punctual Shadow Resolution | 1024 |
| Shadow Cascades | 3 |

### 3.3. Post-Processing
| Ayar | Değer |
|---|---|
| Grading Mode | HDR |
| Buffer Format | R11G11B10 (8GB VRAM'de yeterli) |

### 3.4. Devre Dışı Bırakılacaklar (RTX 3050 VRAM bütçesi)
| Özellik | Durum | Neden |
|---|---|---|
| Ray Tracing | ❌ Kapalı | VRAM + performans maliyeti yüksek, şimdilik kapsam dışı |
| Screen Space Reflections | ❌ Kapalı | Gezegen yüzeyinde kullanılmıyor |
| Decals | ❌ Kapalı | Texture workflow yok, vertex color kullanılıyor |
| Virtual Texturing | ❌ Kapalı | UV-less workflow |

### 3.5. Volumetric Lighting
| Ayar | Değer |
|---|---|
| Budget | 100,000 |
| Reprojection | ✅ (performans iyileştirme) |

---

## 4. PAKET BAĞIMLILIKLARI (`Packages/manifest.json`)

Şablonla gelen HDRP paketi dışında eklenmesi zorunlu paketler (isim bazında):

- `com.unity.burst`
- `com.unity.collections`
- `com.unity.mathematics`
- `com.unity.inputsystem`
- `com.unity.render-pipelines.high-definition`

> **Versiyon notu:** Unity 6.3 LTS (6000.3.14f1) için paketleri `Package Manager` içindeki **Verified / Recommended** sürümden seç. Ekipte aynı editor patch + aynı paket sürümleri kullanılmalı.

---

## 5. ASSEMBLY DEFINITION YAPISI (.asmdef)

Burst Compiler'ın Job struct'larını doğru derleyebilmesi ve test izolasyonunun sağlanabilmesi için asmdef zorunludur.

```
Assets/
├── Scripts/
│   ├── Physics/
│   │   └── Vortex.Core.Physics.asmdef
│   │       Referanslar: Unity.Burst, Unity.Collections, Unity.Mathematics, Unity.Jobs
│   │
│   ├── Procedural/
│   │   └── Vortex.Core.Procedural.asmdef
│   │       Referanslar: Vortex.Core.Physics, Unity.Mathematics
│   │
│   ├── Rendering/
│   │   └── Vortex.Core.Rendering.asmdef
│   │       Referanslar: Vortex.Core.Physics, Vortex.Core.Procedural
│   │
│   ├── Ship/
│   │   └── Vortex.Game.asmdef
│   │       Referanslar: Vortex.Core.Physics, Unity.InputSystem
│   │
│   ├── Weapons/
│   │   (Vortex.Game.asmdef içinde — ayrı assembly gerektirmez)
│   │
│   └── NAVI/
│       (Vortex.Game.asmdef içinde)
│
└── Tests/
    ├── Editor/
    │   └── Vortex.Tests.Editor.asmdef
    │       Referanslar: Vortex.Core.Physics, Vortex.Core.Procedural
    │       Test çerçevesi: Unity.TestRunner (EditMode)
    │
    └── PlayMode/
        └── Vortex.Tests.PlayMode.asmdef
            Referanslar: Vortex.Game
            Test çerçevesi: Unity.TestRunner (PlayMode)
```

### .asmdef Kuralları
- `Vortex.Core.Physics` hiçbir oyun/UI koduna bağımlı OLAMAZ (bağımlılık yönü tek yön: yukarı)
- `Vortex.Core.Rendering`, Physics ve Procedural'a bağlıdır; tersi yasak
- Dairesel bağımlılık → derleme hatası → asmdef yapısı ihlal edilmiş demektir

---

## 6. KLASÖR YAPISI

```
Assets/
├── Scenes/
│   ├── Prototype/          ← FAZ 1-4 geliştirme sahneleri
│   └── Tests/              ← GeodesicOrbitTest, CollisionTest, TimeWeaponTest
│
├── Scripts/
│   ├── Physics/            ← RelativisticBody, GeodesicIntegrator, GravityWell, PhysicsConstants
│   ├── Procedural/         ← CelestialBodyTemplate, Factory, VoxelDataManager, DynamicPlanet
│   ├── Rendering/          ← StellarBodyRenderer, AtmospherePass, LensingPass
│   ├── Ship/               ← ShipController, CameraFollow
│   ├── Weapons/            ← TimeWeapon, TimeWeaponUI
│   ├── NAVI/               ← NAVIController, DialogueTrigger, DialoguePopup
│   ├── World/              ← SolariRuinSpawner
│   └── Debug/              ← OrbitVisualizer, CollisionTestController, TimeWeaponTestRunner
│
├── Shaders/
│   ├── VoxelDataGenerator.compute
│   ├── MarchingCubesMesher.compute
│   ├── MarchingCubesLUT.hlsl
│   ├── GravitationalLensing.hlsl
│   ├── Atmosphere.hlsl
│   ├── PlanetSurface.shadergraph
│   └── NAVIHologram.shadergraph
│
├── ScriptableObjects/
│   ├── CelestialBodies/    ← .asset template dosyaları
│   └── Dialogue/           ← SO_DialogueLine assetleri
│
├── Prefabs/
│   ├── DynamicPlanet.prefab
│   └── SolariRuins/
│
├── Models/
│   └── Solari/
│
├── Settings/
│   ├── HDRenderPipelineAsset.asset
│   ├── MainVolumeProfile.asset
│   └── InputActions.inputactions
│
└── Tests/
    ├── Editor/
    └── PlayMode/
```

---

## 7. GLOBAL VOLUME (Sahne Başlangıç Ayarları)

Her geliştirme sahnesine eklenecek `Global Volume` bileşeni overrideleri:

| Post-Process | Değer |
|---|---|
| **Tonemapping** | ACES |
| **Bloom** | Intensity: 0.4, Scatter: 0.7, Tint: beyaz |
| **Color Adjustments** → Saturation | -10 (hafif desatürasyon, diorama hissi) |
| **Ambient Occlusion** | Intensity: 0.5, Radius: 2 |
| **Lens Distortion** | Intensity: -0.05 (hafif barrel — sinematik) |
| **Depth of Field** | ❌ Kapalı (prototip aşamasında) |
| **Motion Blur** | ❌ Kapalı (geodezik hızlarda yanıltıcı olur) |

(Bkz: `ART_BIBLE.md` 3-5 ve `GDD.md` 21)

---

## 8. BURST COMPILER AYARLARI

`Edit → Burst AOT Settings`:
- **Enable Compilation**: ✅
- **Safety Checks**: Editor'da ✅ açık, Release'de ❌ kapalı
- **Debug Mode**: ❌ Kapalı (performans testi sırasında)

`GeodesicIntegrator` Job struct üzerindeki zorunlu attribute'lar:
```csharp
[BurstCompile(CompileSynchronously = false, OptimizeFor = OptimizeFor.Performance)]
public struct GeodesicIntegratorJob : IJobParallelFor { ... }
```

Burst derlendiğini doğrulama: `Jobs → Burst Inspector` menüsünden `GeodesicIntegratorJob` seçildiğinde assembly output görünmeli.

---

## 9. GİT SETUP

`.gitignore` dosyasına eklenecek (Unity şablonuna ek):
```
# Burst önbelleği
Library/BurstCache/

# Shader önbelleği
Library/ShaderCache/

# IL2CPP build çıktısı
Temp/
Build/

# OS
.DS_Store
Thumbs.db
```

Branch stratejisi (minimal):
- `main` → stabil, her faz tamamlandığında merge
- `dev` → aktif geliştirme
- `faz-1`, `faz-2` vb. → faz bazlı çalışma dalları (isteğe bağlı)

---

## 10. KURULUM DOĞRULAMA TESTİ (İlk Açılışta)

Proje ilk açıldığında şu kontroller yapılmadan FAZ 1 kodlamasına başlanmaz:

- [ ] Konsol: HDRP hatası yok (kırmızı error yok)
- [ ] `Edit → Project Settings → Physics → Gravity` = (0,0,0) ✓
- [ ] `Edit → Project Settings → Player → Color Space` = Linear ✓
- [ ] `Window → Package Manager` → Burst, Collections, Mathematics, Input System ✓
- [ ] Boş sahne oluştur → oynat → FPS > 60 ✓
- [ ] Yeni C# scripti oluştur, Burst attribute ekle, derleme hatası yok ✓
- [ ] `Jobs → Burst Inspector` açılıyor ✓

---

*Kurulum sırasında bir ayar değiştirilirse veya paket versiyonu güncellenir ise bu belgede önce kayıt yapılır. (Bkz: `agent.md` GUNCELLEME KURALI)*
