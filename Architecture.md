# Dosya: architecture.md
# VORTEX EVRENİ — YAZILIM VE SİSTEM MİMARİSİ 

> **Ajan Notu:** Bu belge, `PRD.md` (ozellikle Bolum 4: Genel Gorelilik Motoru) ve `LORE.md` (Vortex katman yapisi) referans alinarak olusturulmustur. Unity'nin standart fizik motoru tamamen devre disi birakilacak ve tum core mimari sifirdan insa edilecektir.

## 1. TEMEL TEKNOLOJİ YIĞINI (TECH STACK)
* **Oyun Motoru:** Unity 6.3 LTS (6000.3.14f1).
* **Render Boru Hattı:** HDRP (High Definition Render Pipeline) - Atmosferik raymarching ve ekran uzayı merceklenme efektleri için zorunludur.
* **Optimizasyon Çatısı:** Unity Job System ve Burst Compiler (RK4 Geodezik Entegrasyonunun çoklu çekirdeğe dağıtılması için).
* **GPU Hesaplamaları:** Compute Shaders (HLSL) - Marching Cubes SDF üretimi ve Vertex Color hesaplamaları için.

## 2. FİZİK VE HAREKET MİMARİSİ (VORTEX GEODESIC ENGINE)

Standart `Rigidbody` bileşeni hiçbir dinamik objede kullanılmayacaktır. Hiyerarşi aşağıdaki gibi olacaktır:

### 2.1. `RelativisticBody` (Core Component)
Her dinamik obje (gemiler, oyuncu, asteroitler) bu ana bileşene sahip olacaktır.
* **Veri Durumu:** `CoordinateTime` (t), `ProperTime` ($\tau$), `LocalDeltaTime`, `SphericalPosition` (r, $\theta$, $\phi$), `FourVelocity` ($dx^\mu/d\tau$).
* **Sorumluluk:** Kendi "Proper Time" akisimi yonetmek. `PRD.md` 4.3'teki zaman dondurma mekanigi tetiklendiginde `ProperTime` akis carpani sifira esitlenir (Bkz: `GDD.md` 14.1).
* **Zaman Kurali:** Hareket, animasyon ve cooldown hesaplari global `Time.timeScale` ile degil, obje bazli `LocalDeltaTime` ile yurur.

### 2.2. `GeodesicIntegrator` (System Manager)
Sahnedeki tekil fizik yöneticisidir (Singleton pattern kullanılmamalı, scene-context içinde tutulmalıdır).
* **Sorumluluk:** Her `CustomFixedUpdate` adımında (Unity'nin FixedUpdate'i içinde manuel çağrılır), sahadaki tüm `RelativisticBody` bileşenlerini toplamak.
* **Job System Entegrasyonu:** Tüm gövdelerin Schwarzschild metriğine göre Christoffel sembollerini hesaplamak ve Runge-Kutta 4 (RK4) integrasyon adımını Unity Job System (Burst derlemeli) üzerinden eşzamanlı çözmek.
* **Mapping:** Çözüm sonrası elde edilen yeni küresel koordinatları, Kartezyen (x,y,z) uzayına çevirip objelerin Unity `Transform` bileşenlerini güncellemek.
* **Referans Notu:** Geodezik denklemler ve kabul kriterleri icin Bkz: `PRD.md` 4.1 ve `PRD.md` 5.

### 2.3. `GravityWell` (Kütle Merkezi Bileşeni)
Gezegenlere ve yıldızlara eklenen, metriğin bükülme oranını ($r_s = 2GM/c^2$) belirleyen statik veya yarı-dinamik veri bileşeni. 
* **Olay Ufku Koruması:** `PRD.md` 4.2 geregi, $r_s$ degeri gezegenin fiziksel mesh yaricapindan her zaman en az 10 birim daha kucuk olacak sekilde `OnValidate` veya `Awake` icinde sinirlandirilacak (clamp). Bu kural Faz 1 test kapisidir (Bkz: `BACKLOG.md` Faz 1).

### 2.4. Hiz Siniri ve Time Dilation Birlesimi
* **Işık Hızı Politikası:** Oyun içi ışık hızı $c$ sert üst sınırdır; `RelativisticBody` hızları hard clamp ile $0.95c$ altında tutulur.
* **Soft Limit:** $0.85c$ üstünde itki eğrisi düşürülerek oyuncu kontrollü şekilde limite yaklaşır.
* **Birleşik Çarpan:** `LocalDeltaTime` hız temelli (Lorentz) ve kütle/mesafe temelli (Schwarzschild) çarpanların birleşiminden üretilir.
* **Global Timescale:** `Time.timeScale` çekirdek fizik akışına bağlanmaz; yalnızca sinematik ve geçici görsel etki katmanında kullanılabilir.

## 3. PROCEDURAL GOK CISMI MIMARISI (TEMPLATE + FACTORY + SDF)

Sistemdeki tum gok cisimleri (gezegen, uydu, yildiz, notron yildizi, karadelik, superdev vb.) tek bir kalip mimarisi ile uretilecektir.

### 3.0. `CelestialBodyTemplate` (ScriptableObject Kalibi)
Her tip icin parametre araliklari tutan veri kalibidir. Kaliplar runtime'da random secilip, seed tabanli deterministik degerlerle instantiate edilir.
* **Zorunlu Alanlar:** `BodyClass`, `MassRange`, `RadiusRange`, `DensityRange`, `RotationRange`, `TemperatureRange`, `AlbedoRange`.
* **Uretim Modu:** `SolidSdf`, `GasBand`, `StellarPlasma`, `CompactObject`, `AccretionDisk` gibi mod bayraklari.
* **Fizik Profili:** `HasSurface`, `HasAtmosphere`, `HasEventHorizon`, `SupportsLanding`, `RadiationHazard`.
* **Render Profili:** Renk paleti slotlari, emissive araliklari, shader feature anahtarlari ve LOD limitleri.
* **Referans Notu:** Sinif sahasi ve deterministik akisin teknik tanimi icin Bkz: `PRD.md` 3.4-3.5; gorsel sinif matrisi icin Bkz: `ART_BIBLE.md` 12.1.

### 3.0.1. `CelestialBodyFactory` (Seeded Uretim Yoneticisi)
`SystemSeed + SectorSeed + LocalIndex` kombinasyonundan deterministik bir alt-seed uretir.
* Agirlikli template secimi (oyun bolgesi, fraksiyon etkisi, hikaye fazi).
* Secilen template uzerinden parametre ornekleme ve dogrulama (min-max clamp).
* Cisim tipine gore uygun olusturma hattina yonlendirme.
* **Referans Notu:** Deterministik akis kurallari icin Bkz: `PRD.md` 3.5; uygulama sirasi icin Bkz: `BACKLOG.md` Faz 1.

### 3.1. `VoxelDataGenerator` (Compute Shader)
* `CelestialBodyFactory` tarafindan `SolidSdf` modunda secilen cisimlerde kullanilir (gezegen, uydu, bazi asteroid varyantlari).
* `prd.md` Bolum 3.2'deki gurultu (noise) fonksiyonlarini GPU uzerinde calistiran ana compute shader.
* Çıktı olarak 3 boyutlu bir `ComputeBuffer<float>` (Signed Distance Field değerleri) döndürür.
* **Referans Notu:** Noise katman parametreleri icin Bkz: `PRD.md` 3.2.

### 3.2. `MarchingCubesMesher` (Compute Shader)
* SDF verisini alıp `AppendStructuredBuffer<Triangle>` kullanarak üçgen ağlarını oluşturur.
* **Hibrit Shading Kuralı:** `prd.md` 3.1 ile uyumlu şekilde, keskin kırık/yarık bölgelerinde sert kenar korunur; geniş yüzeylerde yumuşatılmış normal geçişleri kullanılır.
* Height/Slope tabanlı Vertex Color verisi (`prd.md` 3.3) bu aşamada üçgenin her bir köşesine (vertex) yazılır.
* **Referans Notu:** Gorsel kalite kapilari icin Bkz: `ART_BIBLE.md` 6 ve 10.

### 3.3. `DynamicPlanet` (Yönetici Bileşen)
* Gezegenin LOD (Level of Detail) durumunu yönetir. **MVP ve Vertical Slice için varsayılan strateji:** karmaşık bir Octree yerine basit mesafe bazlı 3 kademeli (High/Mid/Low poly) mesh güncellemeleri kullanılacaktır (daha düşük CPU karmaşıklığı ve daha öngörülebilir GPU maliyeti).
* **Ölçekleme Kuralı:** Profilleme sırasında hedef FPS/frametime eşiği korunamıyorsa, yalnızca problemli bölgelerde Octree tabanlı chunk-Lod ve stream yaklaşımı Faz 3 sonrası devreye alınır.
* **Metaball & Yıkım:** İki `DynamicPlanet` objesinin etki alanları (Bounding Sphere) kesiştiğinde, `VoxelDataGenerator` shader'ına iki merkezin de koordinatları gönderilir. Shader içinde, mesafeye bağlı olarak `Smin` (hamurlaşma) veya çıkarma (yıkım) operasyonları tetiklenip mesh yeniden oluşturulur.
* **Referans Notu:** Hamurlasma/yikim oyunsal hedefi icin Bkz: `GDD.md` 3.4 ve `BACKLOG.md` Faz 3.

### 3.4. `StellarBodyRenderer` ve `CompactObjectRenderer`
SDF mesh kullanmayan gok cisimleri icin ozel render yoludur.
* **Yildiz/Superdev:** Emissive sphere + corona/noise katmanlari + isik hacmi.
* **Notron Yildizi:** Yuksek emissive cekirdek, dusuk yaricap, asiri yogun lensing etkisi.
* **Karadelik:** Olay ufku gorsel modeli + accretion disk + ekran uzayi lensing.
* Bu cisimler de `GravityWell` verisiyle fiziksel metrikte aktif kalir.
* **Referans Notu:** Sinif bazli gorsel beklentiler icin Bkz: `ART_BIBLE.md` 12.1 ve `GDD.md` 21.

## 4. GÖRSEL EFEKT VE RENDER MİMARİSİ

### 4.1. Kütleçekimsel Merceklenme (Lensing)
* **Tip:** HDRP Custom Post-Process Volume.
* **Mantık:** Ekran uzayında (Screen-space), pixel UV koordinatlarını ekrandaki `GravityWell` merkezlerine doğru, merkeze yaklaştıkça artan eksponansiyel bir eğriyle saptıran bir fragment shader. Işığın kara delik etrafında bükülmesini simüle eder.
* **Referans Notu:** Oynanis etkisi icin Bkz: `GDD.md` 13.3 ve `ART_BIBLE.md` 4.

### 4.2. Raymarching Atmosfer
* **Tip:** Gezegen mesh'inin üzerine biraz daha büyük bir Sphere Mesh giydirilerek çalışan Custom Shader Graph (veya HLSL).
* **Mantık:** Kameradan çıkan ışının küre içindeki yolunu hesaplayıp yoğunluğa göre Rayleigh (mavi/kızıl saçılma) ve Mie (güneş parlaması) saçılmalarını hesaplar. Kompakt gezegenlere gerçekçi bir derinlik katar.
* **Referans Notu:** Sanat hedefi icin Bkz: `GDD.md` 21 ve `ART_BIBLE.md` 4.

## 5. VERİ VE DURUM YÖNETİMİ (SAVE/LOAD)
Oyun procedural olsa da bir "Seed" ve "Durum" takibi gerektirir.
* **Seed Management:** Evren üretimi için global bir `SystemSeed` kullanılır. Ezo, Keron harabeleri ve anomaliler bu seed'e bağlı olarak deterministik konumlandırılır.
* **State Save:** Parçalanmış bir gezegenin veya zamanı dondurulmuş bir alanın koordinatları/durumları JSON formatında `WorldStateData` olarak serileştirilerek kaydedilir.

## 6. SİSTEMLER ARASI İLETİŞİM (EVENTS)
Bileşenlerin birbirini sıkı sıkıya bağlamasını (tight-coupling) önlemek için `ScriptableObject` tabanlı bir Event (Olay) sistemi kullanılacaktır.
* Örnek: `OnTimeFrozenEvent` fırlatıldığında;
    * `GeodesicIntegrator` hedef alanın $\tau$ akışını durdurur.
    * Ses Yöneticisi ortam sesini boğuklaştırır.
    * Post-Process yöneticisi ekranın o bölgesinde bir renk sapması (Chromatic Aberration) başlatır (Bkz: `GDD.md` 14 ve `ART_BIBLE.md` 4).