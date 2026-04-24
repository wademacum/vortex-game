# Dosya: backlog.md
# VORTEX EVRENİ — GELİŞTİRME İŞ LİSTESİ (BACKLOG & ROADMAP)

> **Ajan Notu:** Bu backlog, `prd.md` ve `architecture.md` belgelerindeki sistemlerin en güvenli ve izole şekilde test edilerek inşa edilmesi için hazırlanmıştır. Bir faz tamamlanıp stabilite (FPS ve hata) testi yapılmadan diğer faza geçilmeyecektir.

> **YURUTME SIRASI (ZORUNLU):** FAZ 1 -> FAZ 2 -> FAZ 3 -> FAZ 4 -> FAZ 5

---

## FAZ 1: PROSEDUREL URETIM ALTYAPISI + CEKIRDEK FIZIK
*Baslangic sirasi zorunludur: once kalip tabanli prosedurel uretim omurgasi, hemen ardindan geodezik fizik cekirdegi. Bu fazda gorsel cila YASAKTIR (Bkz: `PRD.md` 3.4-3.5 ve `PRD.md` 4.1).*

---

- [ ] **[P0] Görev 1: Unity HDRP Proje Kurulumu** (Bkz: `UNITY_SETUP.md` ve `PHYSICS_REFERENCE.md`)

  **Amaç:** Tüm sistemlerin üzerine inşa edileceği temel Unity ortamını hazırlamak. HDRP pipeline olmadan Compute Shader'lar, raymarching post-process ve screen-space lensing çalışmaz; bu nedenle pipeline seçimi bu adımda kilitlenmeli ve sonradan değiştirilmemelidir.

  **Yöntem:** Unity Hub üzerinden Unity 6.3 LTS (6000.3.14f1) ile yeni bir HDRP şablonu projesi oluştur. `HD Render Pipeline Asset` ve `HD Global Volume` varsayılan ayarlarını koru. `Packages/manifest.json` dosyasında `com.unity.burst`, `com.unity.collections` ve `com.unity.mathematics` paketlerinin Unity 6.3 LTS verified sürümlerini kullan. Git reposu başlat, `.gitignore` ekle (Unity şablonu kullan). İlk sahneyi `Scenes/Prototype` klasörüne kaydet.

  **Çıktı/Beklenti:** Proje açıldığında HDRP hata vermez, konsol temizdir. Burst ve Job System paketleri yüklü ve Assembly Definition olmadan da Burst compile adımı çalışıyor olmalı. Boş sahne > 60 FPS çalışmalı.

  **Kodlanacaklar:** Kod yok; proje/paket konfigürasyonu ve klasör yapısı (`Assets/Scripts/Physics`, `Assets/Scripts/Procedural`, `Assets/Scripts/Rendering`, `Assets/Shaders`, `Assets/ScriptableObjects/CelestialBodies`).

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] Unity 6.3 LTS (6000.3.14f1) HDRP şablonu oluştur
    - [ ] Burst, Collections, Mathematics paketlerini ekle
    - [x] Klasör yapısını kur
    - [x] Git repo başlat, `.gitignore` ekle
    - [ ] Sahneyi kaydet, FPS doğrula

---

- [ ] **[P0] Görev 2: `CelestialBodyTemplate` ScriptableObject Sistemi** (Bkz: `Architecture.md` 3.0 ve `PRD.md` 3.4)

  **Amaç:** Her gök cisminin rastgele değil parametrik olarak üretilmesini sağlayan veri kalıbı altyapısını kurmak. Bu sistem olmadan Factory, Renderer ve SDF pipeline'ları anlamsız ve bağımsız kalır; tüm procedural üretim bu şemaya bağlıdır. Sebastian Lague ilkesi: parametre uzayını tasarla, tek tek cisim modelleme (Bkz: `ART_BIBLE.md` 12.4).

  **Yöntem:** `CelestialBodyTemplate` adında soyut bir `ScriptableObject` base class yaz. Alt sınıflar: `PlanetTemplate`, `MoonTemplate`, `StarTemplate`, `NeutronStarTemplate`, `BlackHoleTemplate`, `SupergiantTemplate`. Her şablonda alanlar: `bodyType` (enum), `massRange` (Vector2), `radiusRange` (Vector2), `noiseLayerConfig` (iç içe struct — `PRD.md` 3.2'deki Kıta/Dağ/Detay parametreleri), `biomeColorCurves` (Gradient array), `anomalyChance` (float). `[CreateAssetMenu]` attribute ile her tip için `.asset` dosyaları oluşturulabilmeli.

  **Çıktı/Beklenti:** Unity Editor'da sağ tıkla → Create → Vortex → CelestialBody → [tip] menüsü çalışmalı. En az bir örnek `PlanetTemplate` ve bir `BlackHoleTemplate` `.asset` dosyası Inspector'dan doldurulabilmeli.

  **Kodlanacaklar:**
  - `Assets/Scripts/Procedural/CelestialBodyTemplate.cs` (abstract base)
  - `Assets/Scripts/Procedural/PlanetTemplate.cs`, `StarTemplate.cs`, `BlackHoleTemplate.cs`, vb.
  - `Assets/Scripts/Procedural/NoiseLayerConfig.cs` (struct)
  - `Assets/ScriptableObjects/CelestialBodies/` altına örnek `.asset` dosyaları

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [x] Base `CelestialBodyTemplate` abstract class yaz
    - [x] Tüm alt tip sınıflarını yaz (6 adet)
    - [x] `NoiseLayerConfig` struct'ını tanımla
    - [x] `[CreateAssetMenu]` attribute'larını ekle
    - [ ] Örnek `.asset` dosyalarını oluştur ve Inspector'dan doldur
    - [ ] Derleme hatası yokluğunu doğrula

---

- [ ] **[P0] Görev 3: `CelestialBodyFactory` Sistemi** (Bkz: `Architecture.md` 3.0.1 ve `PRD.md` 3.5)

  **Amaç:** Seed alan bir fabrika sınıfı ile her çalıştırmada aynı seed → aynı cisim üretilmesini garanti altına almak. Bu deterministik davranış, hem save/load için hem de hata tekrarlanabilirliği için zorunludur. Factory olmadan template'ler statik kalır ve oyunun "her gezegenin biricik ama tutarlı olması" hedefi karşılanamaz.

  **Yöntem:** `CelestialBodyFactory` static sınıfı yaz. İmza: `public static CelestialBodyTemplate Generate(int seed, BodyType type, CelestialBodyTemplate[] pool)`. İçeride `System.Random` yerine `Unity.Mathematics.Random` kullan (Burst uyumluluğu). Önce pool içinden ağırlıklı seçim (her template'in `spawnWeight` float alanı), ardından seçilen template aralıklarından `Unity.Mathematics.Random` ile deterministik değer örnekleme. Üretilen `RuntimeBodyData` struct'ını döndür (ScriptableObject değiştirilmez; çalışma zamanı verisi ayrı tutulur).

  **Çıktı/Beklenti:** Aynı seed ile çağrıldığında her seferinde özdeş `RuntimeBodyData` döner. Farklı seed → farklı ama geçerli veri döner. Birim test (`EditMode`) ile doğrulanabilir.

  **Kodlanacaklar:**
  - `Assets/Scripts/Procedural/CelestialBodyFactory.cs`
  - `Assets/Scripts/Procedural/RuntimeBodyData.cs` (struct)
  - `Assets/Tests/Editor/CelestialBodyFactoryTests.cs` (opsiyonel ama önerilir)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [x] `RuntimeBodyData` struct'ını tanımla
    - [x] `CelestialBodyFactory.Generate()` metodunu yaz
    - [x] `Unity.Mathematics.Random` ile ağırlıklı seçimi implemente et
    - [x] Deterministik parametere örneklemeyi implemente et
    - [ ] Manuel Editor testi ile aynı seed → aynı çıktı doğrula

---

- [ ] **[P0] Görev 4: `GravityWell` Bileşeni** (Bkz: `Architecture.md` 2.3 ve `PRD.md` 4.2)

  **Amaç:** Sahnedeki her büyük kütleli cismin etrafında Schwarzschild metriğine göre tanımlı bir yerçekimi alanı oluşturmak. `GravityWell` olmadan `GeodesicIntegrator`'ın hesaplayacağı bir metrik eğriliği yoktur; bu bileşen tüm fizik zincirinin başlangıç noktasıdır.

  **Yöntem:** `GravityWell : MonoBehaviour` yaz. `public float mass` (Schwarzschild kütle parametresi $M$, SI birimlerinden bağımsız oyun birimi). `public float schwarzschildRadius` property olarak hesaplanır: $r_s = 2GM/c^2$ — $G$ ve $c$ oyun içi sabitler. `GravityWellRegistry` adında static bir liste yöneticisi (ya da `ScriptableObject` event bus) yaz ki integratör tüm aktif kuyuları sorgulayabilsin. Birden fazla `GravityWell` aynı anda sahnede olabilmeli.

  **Çıktı/Beklenti:** Inspector'da `mass` değiştirildiğinde `schwarzschildRadius` anlık güncellenmeli. `GravityWellRegistry.GetAll()` doğru sayıda kuyu döndürmeli. Gizmo olarak Schwarzschild yarıçapı küre wireframe çizilmeli.

  **Kodlanacaklar:**
  - `Assets/Scripts/Physics/GravityWell.cs`
  - `Assets/Scripts/Physics/GravityWellRegistry.cs`
  - `Assets/Scripts/Physics/PhysicsConstants.cs` ($G$, $c$ sabitleri)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [x] `PhysicsConstants.cs` yaz
    - [x] `GravityWell.cs` yaz, Schwarzschild yarıçapını hesapla
    - [x] `GravityWellRegistry` statik listesini yaz
    - [x] OnEnable/OnDisable ile kayıt/çıkış mantığını ekle
    - [x] `OnDrawGizmos` ile wireframe küre ekle
    - [ ] Inspector testi yap

---

- [ ] **[P0] Görev 5: `RelativisticBody` Bileşeni** (Bkz: `Architecture.md` 2.1 ve `PRD.md` 4.1)

  **Amaç:** Sahnedeki her fizik nesnesinin (gemi, gezegen parçası, test objesi) hem Koordinat Zamanı hem de Öz Zamanını (ProperTime) takip etmesini sağlamak. `ProperTime` sıfırlanabileceği için bu bileşen Zaman Manipülasyonu silahının da temel hook'udur (Bkz: `PRD.md` 4.3). Newton `Rigidbody` kullanılmaz; bu bileşen onun yerini alır.

  **Yöntem:** `RelativisticBody : MonoBehaviour` yaz. Alanlar: `float properTime` (Öz Zaman çarpanı, 0–1 aralığı), `float localDeltaTime`, `Vector3 fourVelocity` (4-hız, integratör tarafından güncellenir), `float coordinateTime` (salt okunur, global zaman ile senkron). `Update()` içinde `coordinateTime += Time.deltaTime` ama tüm hareket/animasyon/cooldown adımları `localDeltaTime` ile yürütülür. `FreezeProperTime()` ve `RestoreProperTime()` public metotlar. `IsTimeFrozen` bool property. Rigidbody bileşeni varsa devre dışı bırak.

  **Çıktı/Beklenti:** `FreezeProperTime()` çağrıldığında obje `properTime = 0` olur ve fiziksel hareket durur (ama `coordinateTime` akar). `RestoreProperTime()` ile kaldığı yerden devam eder. NaN fırlatmaz.

  **Kodlanacaklar:**
  - `Assets/Scripts/Physics/RelativisticBody.cs`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [x] `RelativisticBody.cs` temel alanlarla yaz
    - [x] `FreezeProperTime()` / `RestoreProperTime()` ekle
    - [x] `IsTimeFrozen` property ekle
    - [x] `localDeltaTime` alanını ekle ve obje-bazli zaman akisini bununla surdur
    - [x] Rigidbody devre dışı bırakma mantığını ekle
    - [ ] Basit Inspector testi: dondur → `properTime == 0` doğrula

---

- [ ] **[P0] Görev 6 (KRİTİK): `GeodesicIntegrator` Sistemi** (Bkz: `Architecture.md` 2.2 ve `PRD.md` 4.1)

  **Amaç:** Newton yeçekimi kullanmadan, Schwarzschild metriğindeki geodezik denklemleri çözerek her `RelativisticBody`'nin bir sonraki pozisyonunu hesaplamak. Bu sistemin doğruluğu ve performansı tüm oyunun fizik inandırıcılığını belirler; yanlış ya da yavaş olursa diğer hiçbir sistem güvenilir çalışmaz.

  **Yöntem:** `GeodesicIntegrator : IJobParallelFor` olarak Unity Job System ile implemente et. Her frame, tüm `RelativisticBody` listesi üzerinden paralel çalışır. İçerik: (1) `GravityWellRegistry`'den aktif kuyuları `NativeArray<GravityWellData>` olarak al; (2) Schwarzschild Christoffel sembollerini analitik formülle hesapla (türev tablosu, `PRD.md` 4.1'deki formüller); (3) Lorentz + Schwarzschild çarpanlarından obje-bazlı `localDeltaTime` üret; (4) RK4 ile 4-hız ve 4-konum vektörünü $d\tau$ adımı kadar ilerlet; (5) `TransformAccessArray` ile Unity Transform'u güncelle. `[BurstCompile]` attribute zorunlu. `Time.fixedDeltaTime` yalnızca global referans adımıdır; hareket denklemleri doğrudan `Time.timeScale` kullanmaz.

  **Çıktı/Beklenti:** 50 aktif `RelativisticBody` ile sahnede 60 FPS altına düşmemeli (RTX 3050 hedef). Tek cisim testi: küresel simetrik kuyuda başlangıç hızı verildiğinde kapalı eliptik yörünge çizmeli (Newton uyumlu limit). Profiler'da Burst derlenmiş iş parçacıkları görünmeli.

  **Kodlanacaklar:**
  - `Assets/Scripts/Physics/GeodesicIntegrator.cs` (IJobParallelFor)
  - `Assets/Scripts/Physics/GravityWellData.cs` (NativeArray için blittable struct)
  - `Assets/Scripts/Physics/GeodesicSystem.cs` (MonoBehaviour, Job'ları zamanlar)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [x] `GravityWellData` blittable struct yaz
    - [x] `GeodesicIntegrator` Job struct'ını yaz (IJobParallelFor)
    - [x] Christoffel sembollerini Schwarzschild metriği için kodla
    - [x] Hiz icin soft clamp (0.85c) ve hard clamp (0.95c) kurallarini implemente et
    - [x] Lorentz + kütle/mesafe carpanlari ile `localDeltaTime` hesapla
    - [x] RK4 adım fonksiyonunu yaz
    - [x] `[BurstCompile]` ekle ve derlemeyi doğrula
    - [x] `GeodesicSystem` MonoBehaviour ile Job'ları zamanla
    - [x] `TransformAccessArray` ile pozisyon güncellemesini entegre et
    - [ ] Tek cisim yörünge testi: eliptik yörünge çizmeli
    - [ ] Profiler testi: 50 cisim @ 60 FPS

---

- [ ] **[P1] Görev 7: Geodezik Yörünge Doğrulama Testi** (Bkz: `PRD.md` 5 madde 2 ve `GDD.md` 13.1)

  **Amaç:** İntegratörün Newton sınırında doğru davrandığını ve Newton fiziği olmadan gerçek geodezik izlerken stabilitenin korunduğunu doğrulamak. Bu test geçmeden faz kapısı açılmaz; FAZ 2'ye sadece bu test onaylandıktan sonra geçilir.

  **Yöntem:** Test sahnesi kur: tek `GravityWell` (büyük $M$), tek `RelativisticBody` test objesi (küçük kütle, başlangıç hızı verilmiş). `LineRenderer` ile yörüngeyi çiz. Parametreler: düşük hız limiti ($v \ll c$) → Newton ile örtüşmeli; yüksek hız → Schwarzschild öncesyonu gözlemlenmeli. Otomatik test değil; görsel doğrulama yeterli. İkinci test: `ProperTime = 0` yap → cisim hareket etmemeli.

  **Çıktı/Beklenti:** `LineRenderer`'da kapalı (veya hafif presesyon gösteren) yörünge eğrisi görünmeli. `ProperTime = 0` durumunda obje ekranda sabit kalmalı, NaN veya Infinity değeri konsola yazılmamalı.

  **Kodlanacaklar:**
  - `Assets/Scenes/Tests/GeodesicOrbitTest.unity` (sahne)
  - `Assets/Scripts/Debug/OrbitVisualizer.cs` (LineRenderer wrapper)
  - `Assets/Scripts/Debug/GeodesicOrbitTestRunner.cs` (senaryo presetleri + freeze/NaN kontrol yardimcisi)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] Test sahnesini kur
    - [x] `OrbitVisualizer` yaz
    - [ ] Düşük hız limiti testi → Newton uyumu gözlemle
    - [ ] Yüksek hız testi → Schwarzschild öncesyonu gözlemle
    - [ ] `ProperTime = 0` freeze testi
    - [ ] NaN/Infinity konsol kontrolü
    - [ ] FAZ kapısı onayı: geçtiyse FAZ 2'ye geç

---

- [ ] **[P1] Görev 8: Basit Gemi Kontrolcüsü (İtki Sistemi)** (Bkz: `GDD.md` 12.2)

  **Amaç:** Oyuncunun geodezik fiziği hissedebilmesi için en ilkel haliyle gemi kontrolünü eklemek. Bu aşamada görsel model, animasyon veya ses yoktur; sadece itki kuvvetinin `fourVelocity`'i doğru şekilde etkilediği doğrulanır. "Yerçekimine rağmen manevra" hissi bu görevde test edilir.

  **Yöntem:** `ShipController : MonoBehaviour` yaz. Input System (yeni Input System paketi, `com.unity.inputsystem`) ile WASD/Space/Shift → itki vektörü. İtki, `RelativisticBody.fourVelocity`'e her `FixedUpdate`'te eklenir (`thrustForce * Time.fixedDeltaTime`). Kamerası: basit bir `CameraFollow` scripti (smooth follow, no Cinemachine henüz). İtki kuvveti Inspector'dan ayarlanabilir. `properTime == 0` iken itki devre dışı olmalı (zaman dondurulmuş gemiye itki uygulanamaz).

  **Çıktı/Beklenti:** Tuşa basınca gemi geodezik yörüngeden ayrılabilmeli. Tuş bırakıldığında tekrar serbest geodezik izlemeli. `properTime == 0` iken kontrol yanıt vermemeli.

  **Kodlanacaklar:**
  - `Assets/Scripts/Ship/ShipController.cs`
  - `Assets/Scripts/Ship/CameraFollow.cs`
  - `Assets/Settings/InputActions.inputactions` (Input System asset)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [x] Input System paketini ekle
    - [x] `InputActions.inputactions` asset oluştur
    - [x] `ShipController.cs` yaz
    - [x] `fourVelocity`'e itki ekleme mantığını entegre et
    - [x] `properTime == 0` iken itki kilidi ekle
    - [x] `CameraFollow.cs` yaz
    - [ ] Sahne testi: geodezikten çık/geri dön

---

## FAZ 2: VORTEX ARAZISI (COMPUTE SHADER & SDF)
*Oyunun ana görsel ve etkileşim karakteristiğini kurma zamanı (Bkz: `Architecture.md` 3.1-3.4 ve `PRD.md` 3.2-3.5).*

---

- [ ] **[P0] Görev 1: `VoxelDataGenerator` Compute Shader** (Bkz: `Architecture.md` 3.1 ve `PRD.md` 3.2)

  **Amaç:** Gezegen yüzeyini tanımlayan SDF (Signed Distance Field) voxel grid verisini GPU üzerinde üretmek. CPU üzerinde üretim çözünürlük-performans dengesini sağlayamaz; bu nedenle tüm voxel hesabı Compute Shader'a taşınmalıdır. Bu shader olmadan Marching Cubes mesh üretemez, gezegen sahnede görünmez.

  **Yöntem:** `VoxelDataGenerator.compute` HLSL Compute Shader yaz. Dispatch grid: 3D dispatch, her thread bir voxel hücresine karşılık gelir. Temel SDF: `float sdf = length(p) - radius`. Ardından `PRD.md` 3.2'deki 3 noise katmanı ekle: Kıta katmanı (düşük frekans FBM, büyük ölçek yerleşim), Dağ katmanı (orta frekans ridge noise, yüksek tepeler), Detay katmanı (yüksek frekans, ince yüzey detayı). Katman ağırlıkları `RuntimeBodyData`'dan gelen parametrelerle belirlenmeli. Sonuç `RWStructuredBuffer<float>` içine yazılır.

  **Çıktı/Beklenti:** Dispatch sonrası buffer okunduğunda merkezde negatif, dışarıda pozitif SDF değerleri bulunmalı. Noise katmanları gezegen çapının %10-40'ı oranında yüzey dalgalanması üretmeli. RTX 3050'de 64³ grid için <5ms olmalı.

  **Kodlanacaklar:**
  - `Assets/Shaders/VoxelDataGenerator.compute`
  - `Assets/Scripts/Procedural/VoxelDataManager.cs` (Compute Buffer yönetimi, dispatch wrapper)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] Temel küre SDF fonksiyonunu yaz
    - [ ] FBM noise fonksiyonunu HLSL içinde implemente et
    - [ ] Kıta / Dağ / Detay katmanlarını ekle
    - [ ] Parametre buffer'ı (ConstantBuffer) ile `RuntimeBodyData` değerlerini aktar
    - [ ] `VoxelDataManager.cs` ile C# tarafında dispatch yönetimini yaz
    - [ ] Buffer readback ile değerleri doğrula (negatif/pozitif dağılım)
    - [ ] Performans: 64³ grid < 5ms hedef

---

- [ ] **[P0] Görev 2: `MarchingCubesMesher` Compute Shader** (Bkz: `Architecture.md` 3.2 ve `PRD.md` 3.3)

  **Amaç:** `VoxelDataGenerator`'dan gelen SDF verisini gerçek zamanlı üçgen mesh'e dönüştürmek. Gezegen yüzeyindeki tüm değişimler (yıkım, hamurlaşma) bu shader'ın yeniden çalıştırılmasıyla güncellenir. CPU Marching Cubes yeterince hızlı değildir; GPU paralel implementasyonu zorunludur.

  **Yöntem:** `MarchingCubesMesher.compute` HLSL Compute Shader yaz. Standart Marching Cubes lookup tablosunu HLSL sabit dizisi olarak tanımla (256 giriş, her birinde üçgen vertex offsetleri). Her thread bir voxel hücresi için tri sayısını belirler ve `AppendStructuredBuffer<Triangle>` içine yazar. C# tarafında `Graphics.DrawMeshNow` veya `Mesh.SetVertices` ile sonucu render buffer'a aktar. Vertex Color ataması: `PRD.md` 3.3'teki biome eğrilerini normal yönü ve SDF derinliğine göre HLSL içinde hesapla (UV yok, tamamen vertex color workflow).

  **Çıktı/Beklenti:** Sahnede SDF küre + noise kombinasyonundan türeyen, dağlık yüzeyli gezegen mesh'i görünmeli. Vertex Color ile biome renk geçişleri gözlemlenmeli. Mesh normalleri doğru hesaplanmış olmalı (SDF gradyanından).

  **Kodlanacaklar:**
  - `Assets/Shaders/MarchingCubesMesher.compute`
  - `Assets/Shaders/MarchingCubesLUT.hlsl` (lookup tablosu include dosyası)
  - `Assets/Scripts/Procedural/MarchingCubesMesher.cs` (C# dispatch ve mesh upload)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] Marching Cubes LUT'unu HLSL sabit dizisi olarak yaz
    - [ ] Her thread için case hesabını ve `AppendStructuredBuffer` yazımını yaz
    - [ ] Normal hesabını SDF gradyanından (merkezi fark) yap
    - [ ] Vertex Color biome atamasını entegre et
    - [ ] C# tarafında mesh asset'e upload pipeline yaz
    - [ ] Sahne testi: gezegen görünür, renk geçişleri gözlemlenir
    - [ ] Wireframe modunda üçgen sayısını ve normal yönlerini doğrula

---

- [ ] **[P0] Görev 3: `DynamicPlanet` Yöneticisi** (Bkz: `Architecture.md` 3.3 ve `PRD.md` 3.5)

  **Amaç:** `VoxelDataGenerator` ve `MarchingCubesMesher` pipeline'larını tek bir MonoBehaviour çatısı altında birleştirerek gezegeni sahnede yaşayan bir obje haline getirmek. Bu bileşen, FAZ 3'teki yıkım ve hamurlaşma operasyonlarının da uygulama noktasıdır.

  **Yöntem:** `DynamicPlanet : MonoBehaviour` yaz. Initialization: `RuntimeBodyData`'yı okur, Compute Buffer'ları tahsis eder, ilk dispatch'i yapar. `RegenerateMesh()` public metod: tüm pipeline'ı yeniden çalıştırır (yıkım sonrası çağrılır). Spawn/Init akışında `GravityWell.ApplyProceduralBody(runtimeData.mass, runtimeData.radius)` çağrısı zorunludur. Marching Cubes çıktısı her yenilendiğinde `MeshFilter` ile birlikte `MeshCollider.sharedMesh` de güncellenerek dağ/tepe/çukur geometrisinde gerçek temas sağlanır. LOD: 3 çözünürlük seviyesi (64³ yakın, 32³ orta, 16³ uzak). `RelativisticBody` bileşeni ile entegre — gezegen de bir `RelativisticBody`'dir. Bellek: her `DynamicPlanet` kendi buffer'larını yönetir, `OnDestroy`'da release eder.

  **Çıktı/Beklenti:** Sahnede `DynamicPlanet` prefab sürüklenerek konumlandırılabilmeli. Kamera yaklaştıkça LOD geçişi görünmeli. `RegenerateMesh()` çağrıldığında mesh anlık güncellenmeli.

  **Kodlanacaklar:**
  - `Assets/Scripts/Procedural/DynamicPlanet.cs`
  - `Assets/Prefabs/DynamicPlanet.prefab`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `DynamicPlanet.cs` yaz (init, dispatch, bellek yönetimi)
    - [ ] `RegenerateMesh()` metodunu yaz    - [ ] Spawn/Init sonunda `GravityWell.ApplyProceduralBody(runtimeData.mass, runtimeData.radius)` çağrısını bağla
    - [ ] Marching Cubes çıktısı güncellendikçe `MeshCollider.sharedMesh` senkronunu ekle (non-sphere yüzey çarpışması için)    - [ ] 3-tier LOD mantığını (kamera mesafesi bazlı) implemente et
    - [ ] `RelativisticBody` bileşeniyle ortak kullanım testini yap
    - [ ] Prefab oluştur
    - [ ] LOD geçiş testi (kamera ileri/geri)
    - [ ] `OnDestroy` buffer release testi (Memory Profiler ile sızıntı yok)

---

- [ ] **[P1] Görev 4: SDF-Dışı Cisimler İçin Renderer Prototipi** (Bkz: `Architecture.md` 3.4 ve `ART_BIBLE.md` 12.1)

  **Amaç:** Yıldızlar, nötron yıldızları ve kara delikler katı mesh değildir; bunlar için raymarching/impostor tabanlı görsel pipeline'ın ilk prototipini kurmak. Sahne ilerledikçe bu cisimler FAZ 5'te tam kalitede işlenecek; buradaki hedef sadece görsel placeholder değil, doğru render mimarisini erken aşamada yerleştirmek.

  **Yöntem:** `StellarBodyRenderer` ve `CompactObjectRenderer` için ayrı HDRP Custom Pass yaz (veya Fullscreen Pass). `StarTemplate` için: parlak merkez nokta + Gaussian bloom halo, renk `RuntimeBodyData.surfaceTemperatureColor`'dan gelir. `BlackHoleTemplate` için: yalnızca accretion disk quad mesh + Schwarzschild yarıçapı sınırını temsil eden keskin karanlık disk (lensing FAZ 5'te gelecek, şimdi sadece geometri). `NeutronStarTemplate`: mavi-beyaz nokta + nabız efekti (sinüs dalgası opacity). `ART_BIBLE.md` 12.1'deki cisim sınıfı matrisine uygun kalmalı.

  **Çıktı/Beklenti:** Test sahnesinde aynı anda bir Yıldız, bir Kara Delik ve bir Nötron Yıldızı görsel olarak birbirinden ayırt edilebilir şekilde render edilmeli. Hiçbiri solid mesh kullanmamalı.

  **Kodlanacaklar:**
  - `Assets/Shaders/StellarBodyShader.shadergraph` (veya `.hlsl` Custom Pass)
  - `Assets/Shaders/CompactObjectShader.shadergraph`
  - `Assets/Scripts/Rendering/StellarBodyRenderer.cs`
  - `Assets/Scripts/Rendering/CompactObjectRenderer.cs`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `StellarBodyRenderer` için yıldız shader prototipini yaz
    - [ ] `CompactObjectRenderer` için kara delik geometri prototipini yaz
    - [ ] Nötron yıldızı nabız efektini ekle
    - [ ] `RuntimeBodyData` renk parametrelerini shader'a bağla
    - [ ] Test sahnesi: 3 cisim tipi aynı anda görsel karşılaştırma
    - [ ] `ART_BIBLE.md` 12.1 matrisine uygunluk kontrolü

---

## FAZ 3: YIKIM VE BÜTÜNLEŞME (HAMURLAŞMA) ETKİLEŞİMİ
*Oyunun en büyük mekanik şovunun entegrasyonu (Bkz: `PRD.md` 4.4 ve `GDD.md` 3.4).*

---

- [ ] **[P0] Görev 1: Çarpışma Rotası Sahne Kurulumu** (Bkz: `PRD.md` 4.4 ve `Architecture.md` 3.3)

  **Amaç:** İki `DynamicPlanet`'in geodezik fizikle birbirlerine doğru hareket ettiği, yıkım ve hamurlaşma testinin yapılabileceği kontrollü bir sahne kurmak. Bu sahne, FAZ 3'teki diğer tüm görevlerin test ortamıdır; yanlış çarpışma hızı veya yanlış kitle oranı seçilirse hamurlaşma efekti görsel olarak test edilemez.

  **Yöntem:** Yeni test sahnesi: `Scenes/Tests/CollisionTest.unity`. İki `DynamicPlanet` prefab, aralarındaki mesafe $\approx 5 \times (r_1 + r_2)$. Her birine `GravityWell` ve `RelativisticBody` ekle; başlangıç hızlarını geodezik integratör çarpışma rotasına sokacak şekilde hesapla (kesiştirici yörünge). Çarpışma anının kontrolü için `TimeScale` slider'ı (Editor UI, Debug only) ekle. Farklı kütle oranları (1:1, 3:1, 10:1) için ayrı prefab varyantı oluştur.

  **Çıktı/Beklenti:** İki gezegen sahnede oynandığında birbirlerine doğru hareket etmeli ve yaklaşık 10-30 saniye içinde temas mesafesine gelmeli. Zaman ölçeği değiştirilerek çarpışma yavaşlatılabilmeli.

  **Kodlanacaklar:**
  - `Assets/Scenes/Tests/CollisionTest.unity`
  - `Assets/Scripts/Debug/CollisionTestController.cs` (TimeScale kontrolü, debug UI)
  - Prefab varyantları: `DynamicPlanet_Small.prefab`, `DynamicPlanet_Large.prefab`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `CollisionTest.unity` sahnesini kur
    - [ ] İki gezegen için başlangıç hızlarını kesiştirici yörüngeye ayarla
    - [ ] `CollisionTestController` debug UI yaz
    - [ ] 3 farklı kütle oranı varyantını test et
    - [ ] Temas mesafesi tespiti için trigger collider ekle

---

- [ ] **[P0] Görev 2: Hamurlaşma (Smooth Minimum SDF Birleştirme)** (Bkz: `PRD.md` 4.4 ve `Architecture.md` 3.3)

  **Amaç:** İki gezegenin temas öncesi SDF alanlarını `Smin` (Smooth Minimum) fonksiyonuyla yumuşakça birleştirerek "hamur gibi birleşme" görsel efektini üretmek. Bu, oyunun en özgün görsel anıdır; yanlış implemente edilirse iki mesh üst üste biner ama hamur efekti oluşmaz.

  **Yöntem:** `VoxelDataGenerator.compute` içine `Smin(float a, float b, float k)` fonksiyonunu ekle: `float h = max(k - abs(a-b), 0.0) / k; return min(a, b) - h*h*k*0.25;`. `DynamicPlanet.RegenerateMesh()` imzasını `RegenerateMesh(DynamicPlanet other = null, float blendFactor = 0)` şeklinde genişlet. `other != null` ise dispatch içinde iki SDF'yi `Smin` ile birleştir. `blendFactor` mesafeye göre 0→1 olur. C# tarafında iki gezegen arasındaki mesafeyi her frame hesapla; eşik altındaysa `RegenerateMesh(other)` çağır.

  **Çıktı/Beklenti:** İki gezegen temas öncesi ($\approx r_1 + r_2$ mesafesinde) aralarında görünür bir "köprü" şekli oluşmalı. Temas anında keskin değil yumuşak bir birleşim görünmeli. `k` parametresi Inspector'dan ayarlanabilmeli.

  **Kodlanacaklar:**
  - `Assets/Shaders/VoxelDataGenerator.compute` (Smin fonksiyonu eklenir)
  - `Assets/Scripts/Procedural/DynamicPlanet.cs` (RegenerateMesh güncellenir)
  - `Assets/Scripts/Procedural/PlanetMergeController.cs` (mesafe takibi, blend yönetimi)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `Smin` fonksiyonunu HLSL içine ekle
    - [ ] İkinci SDF'yi Compute Shader'a buffer olarak aktar
    - [ ] `RegenerateMesh()` imzasını genişlet
    - [ ] `PlanetMergeController` mesafe takibini yaz
    - [ ] `blendFactor` → `k` parametresi mapping'ini ayarla
    - [ ] Sahne testi: köprü şekli ve yumuşak birleşim gözlemle
    - [ ] `k` değerleri aralığı (0.1–2.0) ile görsel kalibrasyon

---

- [ ] **[P0] Görev 3: Yıkım (CSG Subtraction ile Hacim Koparma)** (Bkz: `PRD.md` 4.4 ve `Architecture.md` 3.3)

  **Amaç:** Enerji eşiği aşıldığında gezegen yüzeyinden büyük hacimler kopararak gerçek zamanlı yıkım efekti üretmek. Bu, oyunun savaş mekaniğinin temelidir; yüzeysel görsel hasar değil, SDF üzerinde gerçek hacimsel çıkarma yapılmalıdır.

  **Yöntem:** `VoxelDataGenerator.compute` içine `CSGSubtract(float sceneSDF, float cutterSDF)` ekle: `max(sceneSDF, -cutterSDF)`. Kesen hacim: küre veya Capsule SDF (çarpışma noktasında, `CollisionPoint` + `cutRadius` parametresi). `DynamicPlanet`'e `ApplyDamage(Vector3 worldPos, float radius, float depth)` public metod ekle: buffer içindeki cut parametrelerini günceller, `RegenerateMesh()` çağırır. Çarpışma anı: `CollisionTestController` eşiği geçtiğinde her iki gezegene `ApplyDamage` çağırır.

  **Çıktı/Beklenti:** `ApplyDamage` çağrıldığında gezegen yüzeyinde küresel bir krater oluşmalı. Krater kenarları `Smin` ile yumuşatılabilir. Çoklu `ApplyDamage` çağrıları birikimlı hasar oluşturmalı.

  **Kodlanacaklar:**
  - `Assets/Shaders/VoxelDataGenerator.compute` (CSGSubtract fonksiyonu eklenir)
  - `Assets/Scripts/Procedural/DynamicPlanet.cs` (`ApplyDamage` metodu eklenir)
  - `Assets/Scripts/Procedural/CollisionDamageController.cs`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `CSGSubtract` fonksiyonunu HLSL içine ekle
    - [ ] Cut parametresi buffer'ını yaz
    - [ ] `ApplyDamage()` metodunu `DynamicPlanet.cs`'e ekle
    - [ ] `CollisionDamageController` eşik tetiklemesini yaz
    - [ ] Test: tek krater oluşumu
    - [ ] Test: çoklu birikmeli krater
    - [ ] Kenar yumuşatma (isteğe bağlı Smin ile)

---

- [ ] **[P1] Görev 4: Kopan Parçalara Fizik Entegrasyonu** (Bkz: `Architecture.md` 2.1 ve `PRD.md` 4.3)

  **Amaç:** Yıkımdan kopan büyük parçaların geodezik fizik motoruna dahil edilmesini sağlamak. Parçalar hem kendi yörüngelerini izlemeli hem de `ProperTime` üzerinden zaman dondurma silahıyla durdurulabilmeli. Bu görev olmadan yıkım sonrası sahne statik kalır ve oyunun "havada asılı enkaz" vizyonu karşılanamaz.

  **Yöntem:** Kopan parça tespiti: `ApplyDamage` sonrası SDF topologisinden kopmış adaları bulmak için bağlı bileşen analizi yapılır (veya basit versiyon: belirli krater derinliği aşıldığında elle tetiklenir). Kopan kısım için yeni bir `DynamicPlanet` (veya hafif `DynamicFragment`) objesi oluştur, `RelativisticBody` ekle, çarpışma anındaki hız vektörü + Newton dürtüsü başlangıç `fourVelocity` olarak ata. Ardından integratör bu parçayı da yönetir. `FreezeProperTime()` çağrıldığında parça havada asılı kalır.

  **Çıktı/Beklenti:** Çarpışma sonrası 1-3 büyük parçanın uzaya fırladığı görülmeli. Her parça geodezik yörünge izlemeli. `ProperTime = 0` komutu verildiğinde parçalar havada donmalı.

  **Kodlanacaklar:**
  - `Assets/Scripts/Procedural/DynamicFragment.cs` (hafif versiyon DynamicPlanet)
  - `Assets/Scripts/Procedural/FragmentSpawner.cs` (parça oluşturma ve hız atama)
  - `Assets/Scripts/Procedural/DynamicPlanet.cs` (parça spawn hook'u eklenir)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `DynamicFragment.cs` yaz (DynamicPlanet'in hafif versiyonu)
    - [ ] `FragmentSpawner.cs` yaz: krater derinliğinden parça sayısı hesapla
    - [ ] Başlangıç `fourVelocity` atama mantığını yaz
    - [ ] `RelativisticBody` entegrasyonunu test et
    - [ ] Parça yörünge testi: uzaya dağılıyor mu?
    - [ ] `FreezeProperTime()` testi: parçalar donuyor mu?

---

## FAZ 4: ZAMAN MANİPÜLASYONU (SİLAH) TESTİ
*Zamanın bir araç olarak kullanılabilmesi ve yerçekimi izolasyonunun testi (Bkz: `PRD.md` 4.3 ve `GDD.md` 14.1).*

---

- [ ] **[P0] Görev 1: Zaman Manipülasyonu Silahı / Alan Etkisi** (Bkz: `PRD.md` 4.3 ve `GDD.md` 14.1)

  **Amaç:** Oyuncunun belirli bir alanı veya hedef objeyi seçerek `ProperTime`'ı anlık olarak sıfırlayabileceği silah/araç mekanikini implemente etmek. Bu, oyunun ana teması olan "zamanı bir araç olarak kullanma"nın ilk somut ifadesidir; FAZ 3'teki yıkım ve FAZ 5'teki bulmacalar bu mekaniği kullanır.

  **Yöntem:** `TimeWeapon : MonoBehaviour` yaz. İki mod: (A) Nokta hedefleme — raycast ile hit edilen `RelativisticBody`'nin `FreezeProperTime()`'ını çağır; (B) Alan modu — `sphereRadius` içindeki tüm `RelativisticBody` listesini `Physics.OverlapSphere` ile topla ve hepsine uygula. Input: Mouse0 = ateş, Mouse1 = mod değiştir. `WeaponRange` ve `AreaRadius` Inspector'dan ayarlanabilir. Görsel feedback: hedef alındığında obje üzerinde `OutlineRenderer` veya renk tonu değişimi.

  **Çıktı/Beklenti:** Sol tık ile vurulan obje anında durmalı. Alan modunda yarıçap içindeki tüm cisimler aynı anda durmalı. Görsel feedback açık şekilde gözlemlenmeli.

  **Kodlanacaklar:**
  - `Assets/Scripts/Weapons/TimeWeapon.cs`
  - `Assets/Scripts/Weapons/TimeWeaponUI.cs` (mod göstergesi, basit HUD)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `TimeWeapon.cs` nokta hedefleme modunu yaz
    - [ ] `OverlapSphere` ile alan modunu yaz
    - [ ] Mod geçiş mantığını ekle
    - [ ] Görsel feedback (outline veya renk tonu) ekle
    - [ ] `TimeWeaponUI.cs` basit HUD yaz
    - [ ] Input bağlantısını kur (Input System)
    - [ ] Sahne testi: tek obje durdurma
    - [ ] Sahne testi: çoklu alan modu

---

- [ ] **[P0] Görev 2: `ProperTime = 0` Dondurma Metodu** (Bkz: `Architecture.md` 2.1 ve `PRD.md` 4.3)

  **Amaç:** `RelativisticBody.FreezeProperTime()` metodunun tüm edge case'leri doğru işlediğini ve integratörün donmuş objeye `NaN` veya `Infinity` hesabı yazmadığını kesin olarak garantilemek. Bu güvenlik katmanı olmadan zaman silahı herhangi bir fizik hatasını tetikleyebilir ve oyunu crash edebilir.

  **Yöntem:** `GeodesicIntegrator` Job içine `if (body.IsTimeFrozen) continue;` guard ekle. `FreezeProperTime()` çağrısında objenin `fourVelocity`'sini sakla (cache), `RestoreProperTime()`'da geri yükle. Eğer donmuş objeye `ApplyDamage` çağrılırsa mesh güncellemesi yapılmaz (guard). `NaN` koruma katmanı: RK4 her adımda `math.isnan()` kontrolü, NaN tespit edilirse adımı atla ve log at.

  **Çıktı/Beklenti:** Donmuş objeye `FreezeProperTime()` çağrılması NaN fırlatmaz. `RestoreProperTime()` sonrası obje kaldığı hızla devam eder. Konsol temiz kalır.

  **Kodlanacaklar:**
  - `Assets/Scripts/Physics/GeodesicIntegrator.cs` (freeze guard + NaN koruma)
  - `Assets/Scripts/Physics/RelativisticBody.cs` (velocity cache ekle)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `GeodesicIntegrator`'a freeze guard ekle
    - [ ] `RelativisticBody`'ye velocity cache alanı ekle
    - [ ] `FreezeProperTime()` cache mantığını yaz
    - [ ] `RestoreProperTime()` restore mantığını yaz
    - [ ] NaN koruma katmanını integratöre ekle
    - [ ] Edge case testi: donmuş → restore → NaN yok
    - [ ] Edge case testi: iki kez freeze → hata yok

---

- [ ] **[P1] Görev 3: Zaman Dondurma Kabul Testi** (Bkz: `PRD.md` 4.3 ve `GDD.md` 14.1)

  **Amaç:** Zaman silahının tüm senaryo kombinasyonlarını geçerli davranışla karşıladığını doğrulamak. Bu test geçmeden FAZ 5'e geçilemez; zira GDD Vertical Slice senaryosu havada asılı enkaz ve kontrollü zaman manipülasyonunu gerektirir.

  **Yöntem:** Test senaryoları (manuel, test sahnesi içinde): (1) Yörüngedeki obje dondurulunca ekranda sabit kalır; (2) İvmelenen obje (gemiden bağımsız) dondurulunca anlık durmur; (3) Dondurulmuş iki cismin `Smin` birleşimi hesaplanmaz (mesh güncellenmez); (4) Zaman silahı menzil dışındaki objeyi etkilemez; (5) 5 obje aynı anda dondurulup serbest bırakılınca hepsi ayrı yörünge devam eder. `OrbitVisualizer` tüm senaryolarda aktif tutularak yörünge izi ile doğrulama yapılır.

  **Çıktı/Beklenti:** 5 senaryo da geçmeli. Hiçbir senaryoda NaN, Infinity veya FPS ani düşüşü olmamalı. Bu test FAZ 5 giriş kapısıdır.

  **Kodlanacaklar:**
  - `Assets/Scenes/Tests/TimeWeaponTest.unity`
  - `Assets/Scripts/Debug/TimeWeaponTestRunner.cs` (senaryo toggle'ları için debug UI)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `TimeWeaponTest.unity` sahnesini kur
    - [ ] 5 test senaryosunu sahneye yerleştir
    - [ ] `TimeWeaponTestRunner` debug UI yaz
    - [ ] Her senaryoyu çalıştır ve sonucu belgele
    - [ ] FAZ kapısı onayı: tümü geçtiyse FAZ 5'e geç

---

## FAZ 5: LORE ENTEGRASYONU VE GÖRSEL CİLA (GDD MVP / VERTICAL SLICE)
*Bu faz, ürün seviyesi MVP'dir (Bkz: `GDD.md` 27). Faz 1-4, bu hedefe hazırlayan teknik prototip kapılarıdır.*

---

- [ ] **[P0] Görev 1: Kütleçekimsel Merceklenme (Lensing) Post-Process Shader** (Bkz: `Architecture.md` 4.1 ve `PRD.md` 4.2)

  **Amaç:** Kara delik ve nötron yıldızı gibi yüksek kütleli cisimlerin etrafında gerçek zamanlı screen-space gravitational lensing efekti üretmek. Bu efekt hem oyunun bilimsel inandırıcılığını hem de sinematik kimliğini oluşturur; placeholder veya basit blur kabul edilmez.

  **Yöntem:** HDRP Custom Post-Process Pass olarak `GravitationalLensing.cs` + `GravitationalLensing.hlsl` yaz. Shader içinde: ekran koordinatlarından world ray'e geri dönüş (inverse view-projection), `GravityWell` pozisyonu ve `schwarzschildRadius`'ı Shader Global parametresi olarak al, `PRD.md` 4.2'deki deflection angle formülünü uygula: $\hat{\alpha} = 4GM/c^2 b$ ($b$ = impact parameter). UV offset hesabını deflection angle'dan türet, `SAMPLE_TEXTURE2D` ile color buffer'ı offset UV ile yeniden örnekle. Birden fazla `GravityWell` için döngü (max 4 kuyu). `CompactObjectRenderer`'la birlikte çalışmalı.

  **Çıktı/Beklenti:** Kara delik etrafında arka plan yıldızları bükülmeli. Schwarzschild yarıçapı sınırı içindeki UV tamamen siyah olmalı (event horizon). FPS düşüşü < 3ms (RTX 3050).

  **Kodlanacaklar:**
  - `Assets/Shaders/GravitationalLensing.hlsl`
  - `Assets/Scripts/Rendering/GravitationalLensingPass.cs`
  - `Assets/Scripts/Rendering/GravitationalLensingVolume.cs` (HDRP Volume Component)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] HDRP Custom Post-Process altyapısını kur
    - [ ] `GravitationalLensing.hlsl` temel deflection hesabını yaz
    - [ ] Schwarzschild event horizon siyahlığını ekle
    - [ ] Çoklu GravityWell döngüsünü ekle (max 4)
    - [ ] Volume Component bağlantısını kur
    - [ ] Sahne testi: UV bükülmesi gözlemlenir
    - [ ] Performans: <3ms GPU time

---

- [ ] **[P0] Görev 2: Raymarching Atmosfer Shader** (Bkz: `Architecture.md` 4.2 ve `ART_BIBLE.md` 12.1)

  **Amaç:** Gezegenler için ikonik renkli atmosfer halesini raymarching ile üretmek. Bu efekt diorama görsel dilinin ve gezegen biricikliğinin temel taşıdır; basit sprite veya sahte bloom kabul edilmez.

  **Yöntem:** HDRP Custom Pass olarak `AtmospherePass.cs` + `Atmosphere.hlsl` yaz. Raymarching: kameradan gezegen merkezine doğru ışın ilerlet, her adımda atmosfer yoğunluğunu örnekle (üstel yoğunluk profili: $\rho(h) = \rho_0 e^{-h/H}$). Işık saçılımı: tek Rayleigh saçılım terimi (basitleştirilmiş). Gezegen `RuntimeBodyData.atmosphereColor` ve `atmosphereThickness` parametrelerini shader'a aktar. Her `DynamicPlanet`'in kendi `AtmosphereVolume` bileşeni olmalı.

  **Çıktı/Beklenti:** Gezegen üst atmosferinde yumuşak renk halesini görülmeli. Renk `RuntimeBodyData`'dan geldiği için her gezegen farklı atmosfer rengine sahip olmalı. Gece tarafında atmosfer sönük, gündüz tarafında parlak görünmeli.

  **Kodlanacaklar:**
  - `Assets/Shaders/Atmosphere.hlsl`
  - `Assets/Scripts/Rendering/AtmospherePass.cs`
  - `Assets/Scripts/Rendering/AtmosphereVolume.cs`
  - `Assets/Scripts/Procedural/DynamicPlanet.cs` (AtmosphereVolume bağlantısı eklenir)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] HDRP Custom Pass altyapısını kur
    - [ ] Üstel yoğunluk profili ve raymarching döngüsünü yaz
    - [ ] Rayleigh saçılım approximation'ı ekle
    - [ ] `RuntimeBodyData` parametre bağlantısını kur
    - [ ] `AtmosphereVolume` MonoBehaviour yaz
    - [ ] `DynamicPlanet` entegrasyonunu ekle
    - [ ] Test: 3 farklı renk ve kalınlıkta atmosfer

---

- [ ] **[P1] Görev 3: Diorama Görsel Dili Uygulaması** (Bkz: `ART_BIBLE.md` 1-6 ve `GDD.md` 21)

  **Amaç:** `ART_BIBLE.md`'de tanımlanan yumuşak sinematik ışık, hibrit shading ve kontrollü bloom görsel dilini tüm sahnede tutarlı olarak uygulamak. FAZ 1-4'te görsel kalite hedef değildi; bu görevde prototype görselinden MVP görsel kimliğine geçiş yapılır.

  **Yöntem:** HDRP Global Volume ayarları: Bloom intensity 0.3-0.6 arası, `Lens Distortion` hafif, `Color Adjustments` ile satürasyon -10 (hafif desatürasyon), `Tonemapping` ACES. `DynamicPlanet`'in HDRP Lit Shader'ı için custom Surface Shader yaz: vertex color → Albedo, SDF normali → smooth+sharp hibrit (`lerp(flatNormal, smoothNormal, 0.6)`). Yıldızlar ve arka plan için `Starfield` Fullscreen Custom Pass. Directional Light: `ART_BIBLE.md` 3'te belirtilen yumuşak açılı, düşük sertlik (`Shadow Softness > 0.5`).

  **Çıktı/Beklenti:** Sahneler referans görsel (diorama estetik) ile karşılaştırıldığında tutarlı hissettirmeli. Art Director onayı gereklidir (kullanıcı onayı). `ART_BIBLE.md` 12 kontrolü yapılacak.

  **Kodlanacaklar:**
  - `Assets/Shaders/PlanetSurface.shadergraph` (hibrit shading)
  - `Assets/Shaders/Starfield.hlsl` (arka plan geçidi)
  - HDRP Volume ayar profili: `Assets/Settings/MainVolumeProfile.asset`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] HDRP Volume parametrelerini kalibre et
    - [ ] `PlanetSurface.shadergraph` hibrit shading yaz
    - [ ] Vertex color → Albedo pipeline'ı bağla
    - [ ] `Starfield` Fullscreen Pass yaz
    - [ ] Directional Light ayarlarını düzenle
    - [ ] Referans görsel ile karşılaştırma (kullanıcı onayı)
    - [ ] `ART_BIBLE.md` 12 kontrolü

---

- [ ] **[P1] Görev 4: 3 Farklı Gezegen Kimliği Üretimi ve Testi** (Bkz: `PRD.md` 3.2-3.5 ve `ART_BIBLE.md` 12.3)

  **Amaç:** `CelestialBodyFactory` + `CelestialBodyTemplate` + `DynamicPlanet` pipeline'ının end-to-end çalışmasını ve üretilen her gezegenin görsel/fiziksel olarak birbirinden gerçekten ayrıştığını doğrulamak. Bu, procedural sistem vaadidir; 3 gezegen aynı görünürse sistem başarısız demektir.

  **Yöntem:** 3 farklı seed ve 3 farklı template kombinasyonu: (A) Volkanik Keron gezegeni (yüksek detay noise, turuncu-kırmızı biome palette); (B) Solari Harabesine ev sahipliği yapan buzul dünya (düşük kıta, beyaz-altın palette); (C) Anomali gezegeni (yüksek anomalyChance, beklenmedik renk patlamaları). Her birini aynı sahneye koy, kamera ile tek tek dolaş. `ART_BIBLE.md` 12.3 random tutarlılık kurallarını kontrol et: aynı seed → aynı gezegen.

  **Çıktı/Beklenti:** 3 gezegen görsel olarak tanınabilir biçimde farklı olmalı. Aynı seed ile yeniden oluşturulan her gezegen özdeş görünmeli. Atmosfer renkleri de farklılaşmalı.

  **Kodlanacaklar:**
  - `Assets/ScriptableObjects/CelestialBodies/VolkanikKeronTemplate.asset`
  - `Assets/ScriptableObjects/CelestialBodies/BuzulDunyaTemplate.asset`
  - `Assets/ScriptableObjects/CelestialBodies/AnomaliTemplate.asset`
  - `Assets/Scenes/Tests/PlanetVarietyTest.unity`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] 3 template `.asset` dosyasını oluştur ve doldur
    - [ ] `PlanetVarietyTest.unity` sahnesi kur
    - [ ] 3 gezegeni aynı sahnede render et
    - [ ] Görsel farklılık değerlendirmesi
    - [ ] Deterministik test: aynı seed → aynı sonuç
    - [ ] `ART_BIBLE.md` 12.3 uyum kontrolü

---

- [ ] **[P1] Görev 5: NAVI (AI) Arayüzü ve İlk Diyalog Testi** (Bkz: `GDD.md` 10 ve `LORE.md` 7)

  **Amaç:** Oyuncunun güvenmemesi gereken yol göstericinin holografik arayüzünü ve ilk manipülatif diyalog tetiklemesini eklemek. Bu görev, `LORE.md` 7'deki NAVI karakter tasarımını ve `GDD.md` 10'daki AI companion sistemini somutlaştırır. Sadece ses/UI yok; oyuncunun NAVI'nin yanlış bilgi verdiğini sezebileceği ilk dramatik an kurulur.

  **Yöntem:** `NAVIController : MonoBehaviour` yaz. Holografik UI: HDRP Unlit shader, yanıp sönen frekans çizgileri (sinüs dalgası vertex shader), mavi-beyaz renk (`ART_BIBLE.md` 7 Keron estetiği). Diyalog sistemi: basit `DialogueTrigger` → `DialoguePopup` pipeline (Unity UI Toolkit veya UGUI). Tetikleme: oyuncu `TriggerZone`'a girince ilk diyalog mesajı. İlk mesaj içeriği (`LORE.md` 7'den): NAVI yanlış rota verir — doğru rotanın tam tersini işaret eder (oyuncu fark edebilir ama zorunlu değil). Her diyalog satırı `SO_DialogueLine` ScriptableObject ile yönetilir.

  **Çıktı/Beklenti:** NAVI holografik UI sahne yüklendiğinde görünür. Trigger zone'a girildiğinde diyalog kutusu açılır. Diyalog içeriği `LORE.md` 7 ile tutarlı. UI kapatılabilir ve tekrar açılabilir.

  **Kodlanacaklar:**
  - `Assets/Scripts/NAVI/NAVIController.cs`
  - `Assets/Scripts/NAVI/DialogueTrigger.cs`
  - `Assets/Scripts/NAVI/DialoguePopup.cs`
  - `Assets/ScriptableObjects/Dialogue/SO_DialogueLine.cs`
  - `Assets/Shaders/NAVIHologram.shadergraph`
  - `Assets/ScriptableObjects/Dialogue/NAVI_FirstContact.asset` (ilk diyalog verisi)

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] `SO_DialogueLine` ScriptableObject tanımla
    - [ ] `NAVIHologram.shadergraph` yaz (yanıp sönen hologram)
    - [ ] `NAVIController.cs` yaz
    - [ ] `DialogueTrigger` ve `DialoguePopup` yaz
    - [ ] `NAVI_FirstContact.asset` diyalog içeriğini `LORE.md` 7'den doldur
    - [ ] Trigger zone testi: diyalog açılıyor mu?
    - [ ] UI kapat/aç testi

---

- [ ] **[P2] Görev 6: İlk Solari Harabesi Asset Yerleştirme** (Bkz: `LORE.md` 8-9 ve `ART_BIBLE.md` 7)

  **Amaç:** `LORE.md` 8-9'da tanımlanan Solari mimarisinin (altın/beyaz biyoteknolojik) Vertical Slice sahnesinde en az bir harabesi olarak görünmesini sağlamak. Bu görev P2 önceliklidir çünkü oynanış mekaniklerini bloklamaz; ancak anlatı bağlamını ve görsel çeşitliliği sağlar.

  **Yöntem:** 3 seçenek: (A) Blender'da basit modüler Solari mimari parçaları üret (sütun, kemer, panel — `ART_BIBLE.md` 7'deki altın/beyaz paleti); (B) Unity ProBuilder ile hızlı placeholder geometri; (C) Serbest asset (geçici). Sahneye `SolariRuinSpawner` scripti ekle: belirli gezegen yüzey koordinatına yapışır (gezegen dönmesi ile birlikte hareket eder). `DynamicPlanet`'in yüzey normali boyunca yerleştirilir. Yıkım sırasında harabenin de SDF'den etkilenebilmesi için `GravityWell`'e kayıtlı olması gerekmez — sadece `RelativisticBody` olmayan statik geometri yeterli.

  **Çıktı/Beklenti:** Sahneye girildiğinde Solari mimarisi görsel olarak `ART_BIBLE.md` 7 ile uyumlu. Gezegen döndüğünde (veya hareket ettiğinde) harabe de gezegen ile birlikte hareket eder.

  **Kodlanacaklar:**
  - `Assets/Models/Solari/` (model dosyaları)
  - `Assets/Scripts/World/SolariRuinSpawner.cs`
  - `Assets/Prefabs/SolariRuins/SolariColumn.prefab`, `SolariArch.prefab`

  **İlerleme:**
  - Yapıldı: —
  - Yapılacak:
    - [ ] Modelleme yöntemini seç (Blender/ProBuilder/asset)
    - [ ] En az 2 modüler parça üret (sütun, kemer)
    - [ ] Prefab'lara HDRP malzeme ata (altın/beyaz paleti)
    - [ ] `SolariRuinSpawner.cs` yaz
    - [ ] Gezegen yüzeyine yerleştirme ve normal hizalama testi
    - [ ] Gezegen hareketli iken harabe birlikte hareket testi
    - [ ] `ART_BIBLE.md` 7 görsel uyum kontrolü

---

## BAŞARI KRİTERİ (VERTICAL SLICE / GDD MVP)
Oyuncu, Ezo gezegeninden çıkıp, Newton fiziği olmayan Geodezik bir yörünge izleyerek, havada asılı duran devasa gezegen yıkıntıları (SDF enkazları) arasından Solari Düğümü'ne ulaşmalı ve bu sırada NAVI'nin ona bilerek yanlış bilgi verdiğini sezecek ilk diyalogu okumalıdır (Bkz: `GDD.md` 27 ve `LORE.md` 7-10).