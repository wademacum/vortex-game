# Dosya: prd.md
# VORTEX (Geçici İsim) - Sistem Tasarım ve Gereksinim Belgesi (PRD)

## 1. Ürün Vizyonu ve Teknik Kapsam
Vortex, Genel Görelilik kurallarını (Uzay-zaman bükülmesi, Zaman Genişlemesi) ve procedural yıkılabilir çevre mekaniklerini (Marching Cubes / SDF) merkeze alan, "kompakt ölçekli" bir uzay keşif/bulmaca oyunudur.
* **Temel Felsefe:** Newton Fiziği ve $F=ma$ mantığı YASAKTIR. Yerçekimi bir kuvvet değil, kütlenin uzay-zamanı bükmesi (Schwarzschild Metriği) ve objelerin bu bükülmüş uzayda Geodezik eğrileri takip etmesidir.
* **Görsel Felsefe:** Gezegenler gerçekçi boyutlarda değildir. Düşük poligonlu (Low-poly), minyatür diorama hissi veren, yumuşak ışık geçişlerine sahip stilize kompakt kürelerdir (Yarıçap: 50-150 Unity birimi).
* **Referans Notu:** Urun hedef baglami icin Bkz: `GDD.md` 2 ve `GDD.md` 27.

## 2. Hedef Donanım ve Performans Kısıtlamaları
Kodlama mimarisi aşağıdaki donanım limitleri göz önüne alınarak optimize edilmelidir:
* **GPU:** RTX 3050 8GB VRAM (Compute Shader VRAM tahsisleri, grid çözünürlükleri ve Octree derinliği bu 8GB limiti aşmayacak şekilde, VRAM paging'e düşmeden tasarlanmalıdır).
* **RAM:** 32GB DDR4 (Voxel verilerinin RAM üzerinde önbelleklenmesi ve asenkron aktarımı için yeterli alan mevcuttur, Garbage Collector sızıntılarına dikkat edilmelidir).
* **CPU:** i5 12400f (Diferansiyel denklem çözümleri ve RK4 integratörleri Job System + Burst Compiler ile çoklu çekirdeğe dağıtılmalıdır).
* Bu limitlerin faz gecisi kontrolu icin Bkz: `BACKLOG.md` Ajan Notu ve `PROGRESS_REPORT.md` RAPOR GUNCELLEME SAKLAMA KURALI.

## 3. Görsel Dilin Teknik Dekonstrüksiyonu
Kompakt ve low-poly gezegen estetiğini üretmek için izlenecek matematiksel yol haritası:

### 3.1. Mesh Üretimi (Low-Poly Estetiği)
* **Hibrit Shading Kuralı:** Marching Cubes ağları low-poly silueti koruyacak; ancak referans görsel diline uygun yumuşak geçişler için normaller tamamen sertlenmeyecektir.
* Eğim açısı eşiği kullanılan kenar ayrımıyla (angle-based split), yalnızca kırık/yarık/keskin topoloji bölgeleri sert tutulur; geniş arazi yüzeyleri yumuşatılmış normallerle render edilir (Bkz: `Architecture.md` 3.2 ve `ART_BIBLE.md` 6).

### 3.2. Procedural Arazinin Noise (Gürültü) Katmanları
SDF (Signed Distance Field) fonksiyonu, temel bir küre fonksiyonundan (`length(p) - radius`) başlayıp üzerine şu noise katmanlarını ekleyerek oluşturulmalıdır:
1. **Kıta Katmanı:** Düşük frekanslı (Scale: 0.05), yüksek genlikli (Amplitude: 20) Simplex Noise. 
2. **Dağ Katmanı:** Sadece kıta katmanının pozitif olduğu yerlerde maskelenir. Ridged Multifractal Noise (Scale: 0.1, Octaves: 4).
3. **Detay Katmanı:** Düşük genlikli (Amplitude: 2), yüksek frekanslı standart Perlin noise.
* Uygulama bileseni icin Bkz: `Architecture.md` 3.1.

### 3.3. Yükseklik ve Eğim Tabanlı Biyom Renklendirmesi
Ana biyom renkleri Compute Shader içinde hesaplanıp Vertex Color olarak atanmalıdır. Texture kullanımı zorunlu değildir; yalnızca mikro detay (yerleşim izi, hafif yüzey kırılımı) için düşük maliyetli maske/gradient katmanı opsiyoneldir:
* **Derin Su:** Koyu Mavi (Height < Deniz Seviyesi - 5)
* **Sığ Su/Kıyı:** Turkuaz (Deniz Seviyesi - 5 < Height < Deniz Seviyesi)
* **Kumsal:** Kum Rengi (Deniz Seviyesi < Height < Deniz Seviyesi + 2)
* **Çim/Orman:** Koyu Yeşil (Deniz Seviyesi + 2 < Height < Kar Sınırı VE Slope > 0.5)
* **Kaya:** Gri/Kahverengi (Slope < 0.5)
* **Kar/Buz:** Beyaz/Açık Mavi (Height > Kar Sınırı)
* Gorsel kimlik uyumu icin Bkz: `ART_BIBLE.md` 2 ve 6.

### 3.4. Gok Cismi Kalip Sistemi (Template-Driven Procedural Generation)
Sebastian Lague yaklasimina benzer sekilde, her cisim tekil elle uretilmez; ortak bir kaliptan seed tabanli parametreler ile olusturulur.
* **Tek Kalip Sema:** Tum cisim tipleri `CelestialBodyTemplate` veri semasina uyar.
* **Desteklenen Siniflar (ilk paket):** Planet, Moon, Star, NeutronStar, BlackHole, Supergiant, AsteroidCluster.
* **Kalip Alani:**
1. Fizik: kutle, yaricap, yogunluk, donus hizi.
2. Render: palet slotlari, emissive araligi, shader modu.
3. Oynanis: inilebilirlik, radyasyon, anomali riski.
4. Uretim: SDF/noise, gaz katmani, plazma, accretion disk bayraklari.
* Bu semanin runtime karsiligi icin Bkz: `Architecture.md` 3.0-3.4.

### 3.5. Deterministik Uretim Akisi (Random ama Tekrarlanabilir)
1. `SystemSeed` evrenin ana rastgeleligini tanimlar.
2. Her bolge icin `SectorSeed` turetilir.
3. `SectorSeed + BodyIndex` ile cisim-seed uretilir.
4. Agirlikli secimle uygun template secilir.
5. Template icindeki min-max araliklardan parametre orneklenir.
6. Fiziksel gecerlilik kurallari calistirilir (ozellikle olay ufku ve yaricap iliskisi).
7. Cisim tipi uygun pipeline'a gider:
	- Katı cisimler: SDF + Marching Cubes.
	- Yildiz/superdev: emissive/hacimsel shader.
	- Karadelik/notron yildizi: compact object render + lensing agirlikli fizik.
* Bu akis sayesinde ayni seed ile ayni evren tekrar uretilir; farkli seed ile yeni kombinasyonlar elde edilir.

## 4. Oyun Mekaniği Detayları (Core Gameplay Loop)

### 4.1. Genel Görelilik Motoru (Geodesic Integrator)
Unity'nin Öklid uzayının (Cartesian Space) içine, bükülmüş bir uzay-zaman metriği entegre edilecektir. 
* **Metrik:** Schwarzschild Metriği kullanılacaktır. $ds^2 = -(1 - r_s/r)c^2dt^2 + (1 - r_s/r)^{-1}dr^2 + r^2(d\theta^2 + \sin^2\theta d\phi^2)$
* **Zamanın İki Yüzü:** Unity'nin `Time.deltaTime`'ı dış referans zamanı (Coordinate Time - $t$) olarak alınacak. Her dinamik obje kendi iç saatini (Proper Time - $\tau$) metrik üzerinden hesaplayıp tutacaktır.
* **Yerel Zaman Kuralı:** Her obje için `localDeltaTime` ayrı hesaplanır. Hareket, animasyon, cooldown ve entegrasyon adımları bu değer ile yürütülür; global `Time.timeScale` çekirdek fizik için kullanılmaz.
* **Geodezik Hareket:** Ajan, Christoffel Sembollerini hesaplayarak Geodezik diferansiyel denklemini (`d²x^μ/dτ² + Γ^μ_αβ (dx^α/dτ)(dx^β/dτ) = 0`) **Runge-Kutta 4th Order (RK4)** integratörü ile fizik döngüsünde (FixedUpdate) çözmelidir.
* **Koordinat Dönüşümü:** RK4 ile bulunan Küresel Koordinatlar ($r, \theta, \phi$), her frame'de Unity'nin Kartezyen Koordinatlarına ($x,y,z$) geri çevrilip `transform.position`'a atanmalıdır.
* Runtime sistem yoneticisi icin Bkz: `Architecture.md` 2.2.

### 4.2. Kompakt Ölçek ve Olay Ufku (Event Horizon) Yönetimi - KRİTİK UYARI
Gezegenler kompakt (örn. Yarıçap = 100 birim) olduğu için, matematiksel çöküşleri (kara delik oluşumu) engellemek adına ajan şu kısıtlamayı ZORUNLU olarak kodlamalıdır:
* Schwarzschild yarıçapı ($r_s = 2GM/c^2$), **KESİNLİKLE** gezegenin fiziksel (SDF) yarıçapının altında kalmalıdır ($r_s < R_{gezegen} - 10$). 
* Aksi takdirde oyuncu yüzeye inmeye çalışırken olay ufkunu geçecek, metrikteki $1 - r_s/r$ formülü negatif veya sıfır olacağı için oyun motorunda *NaN (Not a Number)* hataları fırlatılacaktır. Kütle ($M$) ve Işık Hızı ($c$) değişkenleri bu kurala göre sınırlanmalıdır.
* Bu kisit `GravityWell` tarafinda uygulanir (Bkz: `Architecture.md` 2.3).

### 4.3. Zaman Dondurma Mekaniği (Metric Manipulation)
* Oyuncu bir hedefin zamanını dondurduğunda, "hız vektörü sıfırlanmaz" (Bu Newton mantığıdır).
* Bunun yerine, hedef alanın içindeki metrik tensörün zaman bileşeni ($g_{00}$) dışarıdan gelen bir faktörle sonsuza yaklaştırılır (veya $\Delta\tau$ çarpanı `0.0` yapılır).
* Bu durumda objenin Kendi Zamanı (Proper Time - $\tau$) akmayı durdurur. Diferansiyel denklem (RK4) çözülmeye devam etse bile $\Delta\tau = 0$ olduğu için obje Kartezyen uzayda ($x,y,z$) Koordinat Zamanına ($t$) göre tamamen kilitlenmiş (donmuş) görünür.
* Oynanis hedefi icin Bkz: `GDD.md` 14.1 ve test adimi icin Bkz: `BACKLOG.md` Faz 4 Gv 3.

### 4.4. Çarpışma ve SDF Hamurlaşması (Metaball Merge)
* İki Marching Cubes cismi birbirine yakınlaştığında, ağ yapıları (mesh) birbirine temas etmeden önce SDF fonksiyonları `Smin` (Polynomial Smooth Minimum) fonksiyonu ile birleştirilmelidir.
* Bu işlem cisimler arasında hamur/sıvı benzeri bir köprü kurar. Çarpışma enerjisi eşiği geçilirse boolean çıkarma (CSG Subtraction) işlemi yapılarak bağımsız voxel enkazları oluşturulur.
* Uygulama fazi icin Bkz: `BACKLOG.md` Faz 3.

### 4.5. Işık Hızı Limiti ve Birleşik Relativity Politikası
* Oyun içi ışık hızı ($c$) sert üst sınırdır; hiçbir obje $c$ değerini geçemez.
* Oynanabilir hızlanma aralığı: $0.6c$-$0.8c$; bu aralıkta zaman genişlemesi hissedilir olmalıdır.
* Soft limit: $0.85c$ üzerinde itki verimi kademeli düşürülür.
* Hard limit: $0.95c$ üzerinde hız clamp uygulanır; sayısal stabilite korunur.
* Nihai yerel zaman çarpanı, hız temelli (özel görelilik) ve kütle/mesafe temelli (genel görelilik) etkilerin birleşiminden hesaplanır.
* `Time.timeScale` yalnızca sinematik/özel VFX anlarında sınırlı kullanılabilir; fizik entegratörünün girdisi olamaz.

## 5. Teknik Doğrulama Kriterleri (Prototip Kapısı)
**Terminoloji Notu:** Ürün seviyesi MVP tanımı, `GDD.md` Bölüm 27'deki **MVP / Vertical Slice** kapsamı olarak kabul edilir. Bu bölümdeki maddeler, o hedefe gitmeden önce çekirdek fizik ve zaman mekaniğinin doğrulanması için teknik kabul kapısıdır.
* Faz sirasi ve uygulama adimlari icin Bkz: `BACKLOG.md` Faz 1-4.

Ajanın teslim etmesi gereken teknik prototip şu şartları sağlamalıdır:
1. Ekranda, Bölüm 3'teki kurallara göre Compute Shader ile üretilmiş, low-poly estetiğinde bir gezegen.
2. Basit bir gemi objesinin, Unity yerçekimi (*Gravity*) **tamamen kapalıyken**, Geodezik Entegratör (RK4) ile hesaplanmış bükülmüş uzay-zaman eğrilerini takip ederek gezegen etrafında stabil bir yörüngeye (Orbit) girebilmesi.
3. Proper Time ($\tau$) çarpanı `0.0` yapıldığında, objenin yörüngedeki o anki konumunda Koordinat Zamanına ($t$) göre donup kalması.