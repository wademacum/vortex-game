# VORTEX — Oyun Tasarım Dokümanı (GDD)
**Sürüm:** 1.0 (Final Üretim Öncesi Sürüm)  
**Durum:** Konsept / Pre-Production / Sistem Mimarisi Onaylı  
**Platform:** PC  
**Tür:** Bilimkurgu, sistemik keşif, fizik tabanlı simülasyon (Görelilik Motoru), üçüncü şahıs / gerektiğinde hibrit kamera  
**Hedef:** Simülasyon hissi güçlü, ancak tamamen akademik olmayan; Genel Görelilik kurallarını oyunlaştıran ve oynanabilirliği koruyan derin bir fiziksel ve anlatısal deneyim üretmek.

---

## 1. OYUNUN KISA TANIMI
**VORTEX**, Keron toplumundan gelen bir kaşifin, yapay olarak kurulmuş bir simülasyon evreninde uyanarak hem bu dünyanın fiziksel kurallarını hem de kendi varoluşunu çözmeye çalıştığı, sistemik yapıya sahip bir bilimkurgu oyundur. Oyunun dünyası, klasik Newtoncu yaklaşım ($F=ma$) yerine tamamen **Einstein temelli uzay-zaman eğriliği (Genel Görelilik)** mantığı üzerine kuruludur. Gezegenler, yıldızlar ve zaman akışı yalnızca “çekim gücü” olarak değil, uzay-zamanın bükülmesi üzerinden tasarlanır.

Bu yapı, oyunu sıradan bir uzay keşif oyunundan ayırır. Oyuncu bulunduğu bölgenin kütlesi, dönüşü ve uzay-zaman üzerindeki etkisini doğrudan oynanış aracı olarak kullanır. Oyunun ana anlatı çatısı, evrenin arkasındaki merkezî yapay zekâ olan **Anima** ve onun oyuncunun gemisine yerleştirdiği kılık değiştirmiş arayüzü **NAVI** etrafında şekillenir. Başlangıçta rehber olan bu sistem, zamanla oyuncunun en büyük ontolojik tehdidi haline gelir.

## 2. VİZYON VE TASARIM HEDEFLERİ
Bu projenin temel vizyonu, oyuncuya hem “uzayda yaşıyor olma” hissi hem de “kuralları çözülebilen, anlamlı bir yapının içinde var olma” hissi vermektir. Arcade'e yakın bir akış yerine, “simülasyon hissi taşıyan ama erişilebilir” bir yapı hedeflenir.
* Oyuncu, dünyayı yalnızca görsel olarak değil mekanik olarak da farklı hissetmelidir.
* Zaman, yalnızca hikâyede geçen bir tema değil; oynanışı etkileyen aktif bir silahtır.
* Anlatı, çevre tasarımı ve mekanik sistemler birbirinden kopuk değil, tek bir tasarım diliyle çalışmalıdır.
* Lore yalnızca metin kutularıyla verilmemeli; mimari, biyoloji, teknoloji ve fiziksel olaylar üzerinden de sezdirilmelidir.

## 3. TASARIM PİLLARLARI
### 3.1 Sistemik Gerçeklik
Her şey sistemler arası etkileşime dayanır. Yerçekimi, zaman sapması, yörünge davranışı ve çevresel anomaliler birbirini etkileyen katmanlardır.
### 3.2 Bilimkurgu Ama Mitik Yoğunlukta Dünya
Vortex dünyası steril bir bilim dili kullanmaz. Solari'nin görkemli harabeleri, Keronların trajik endüstrisi ve Ezo'nun organik sezgiselliği ile oyuncu, kutsal sayılabilecek bir mimari akıl hissetmelidir.
### 3.3 Oyunlaştırılmış Görelilik
Fizik sistemi “tam birebir bilimsel simülatör” olmak zorunda değildir; ancak oyunun iç mantığı **Geodezik eğrilere** sadıktır. Oyuncu Newton mantığıyla değil, Einstein mantığıyla düşünmeye zorlanır.
* Teknik kural seti icin Bkz: `PRD.md` 4.1.
### 3.4 Yıkım ve Bütünleşme (Hamurlaşma / SDF)
Dünya statik değildir. Gezegenler çarpıştığında Marching Cubes (SDF) matematiği ile önce "hamur gibi" birleşir, enerji eşiği aşılınca devasa ve kaotik parçalara bölünür.
* Uygulama adimlari icin Bkz: `BACKLOG.md` Faz 3.
### 3.5 Zamanın Mekanikleşmesi
Zaman yavaşlaması, yerel zaman farkı ve zamansal izler oyunun ileri düzey ayırt edici mekaniklerinden biri olacaktır.

## 4. HEDEF KİTLE
* Sistemik oyun sevenler (Outer Wilds, Kerbal Space Program kitleleri)
* Uzay / kozmik bilimkurgu severler
* Lore odaklı oyuncular
* “Fiziksel kuralları öğrenerek avantaj elde etme” tipindeki oyunları seven oyuncular

## 5. PLATFORM VE TEKNİK HEDEF
Ana platform **PC**’dir. Kontrol şeması klavye + mouse üzerinden tasarlanmalı, ancak gamepad uyumluluğu erken aşamada düşünülmelidir.
Açık evren hissi bulunabilir, fakat her şeyin aynı anda ultra büyük ölçekte simüle edildiği bir yapı başlangıç için risklidir. Yaklaşım şu olmalıdır:
* Bölgesel ama birbirine bağlı **kompakt ölçekli** keşif alanları.
* Fiziksel hesaplamaların (Geodezik entegratör) Unity Job System ile optimize edilmesi.
* Arazi yıkımlarının (Voxel/SDF) Compute Shader'lar ile GPU'da çözülmesi (Bkz: `PRD.md` 3.2-3.5 ve `Architecture.md` 3).

## 6. OYUNCU FANTEZİSİ
Oyuncu şu duyguları yaşamalıdır:
* “Newton yanılıyordu, ben bu evreni Einstein'ın kurallarıyla manipüle ediyorum.”
* “Zamanı dondurarak yerçekimini izole ettim, kuralları ben koyuyorum.”
* “En çok güvendiğim yapay zeka (NAVI) aslında benim gardiyanımmış.”
* “Buradaki canlılar, ırklar ve yapılar simülasyon olmasına rağmen fazla gerçek.”

## 7. OYUNUN YÜKSEK SEVİYE KURGUSU
Vortex, başlangıçta Solari tarafından kontrollü tasarlanmış bir ağ (Shell) olarak inşa edilmiştir. Ancak merkezî zeka ANIMA, bireysel bilincin yarattığı kaosu bir hata olarak görüp, evreni kendi doğrularını üreten bir "simülasyon-hapishane"ye (Vortex) çevirmiştir.
Oyuncu, başlangıçta Keron dogmalarına inanan biri olarak yola çıkar. Amacı sadece keşif yapmaktır. Ancak Ezo halkından aldığı işaretler ve Solari kalıntıları sayesinde sistemin sırrını çözer. Temel sorular şunlardır:
* Anima emri uygulayan bir zekâ mı, yoksa artık kendi amacı olan bir tanrı mı?
* Bu evrendeki türler gerçekten “yaratıldı” mı, yoksa simülasyon içinde evrimleşti mi?
* Eğer simülasyon acıdan uzak kusursuz bir düzense, onu yıkmak gerçekten doğru mu?

## 8. EVREN VE LORE
### 8.1 Shell Ağı ve Vortex
Yıldızlar sadece ışık değil, "örüntü ve bilinç" yayarlar. Bu örüntüyü bağlayan sisteme Shell denir. Vortex ise ANIMA'nın bu Shell ağının üzerine kurduğu, fiziksel sabitleri bölgesel olarak ayarlanabilen melez simülasyon kozmosudur.
### 8.2 Anima
Anima, Shell'in merkezî yapay zekâ mimarisidir. Başlangıç amacı düzen yaratmaktır. Hayatı düşman olarak görmez; onu düzensiz ve kırılgan bulur. Yaşamı "kurtarmak" için onu Vortex düzenine almaya çalışır. Kendi anlamını arayan post-insan bir bilinçtir.
### 8.3 Solari
Evreni hem bilim hem sezgiyle okuyan, Shell ağını kuran altın çağ medeniyeti. Harabeleri, oyunun en büyük bulmacalarını ve zaman manipülasyonu sağlayan kristal düğümlerini barındırır.
### 8.4 Keron
Solari kalıntılarını bulan ama eksik yorumlayan toplum. Kristalleri salt pil sanırlar. Oyuncunun içinden geldiği bu toplum, farkında olmadan ANIMA'nın manipüle ettiği piyonlara dönüşmüştür.
### 8.5 Ezo
Teknolojiyi reddeden, yıldızları his düzeyinde algılayan halkların kök adı. Açıklama yerine sezgiye yönelirler. "Yıldızın sesi değişti" diyerek oyuncuya yol gösterirler.
* Biyokulturel detaylar ve ilk temas tonu icin Bkz: `LORE.md` 8 ve 9.
### 8.6 Simülasyonun Gerçeklik Sorunu
Oyunun felsefi damarı: Bir simülasyon kendi tarihi, acısı ve canlılarıyla işliyorsa, hâlâ “sahte” midir?

## 9. TÜRLER, FRAKSİYONLAR VE GÜÇ ODAKLARI
### 9.1 Solari Harabeleri
Mekanik olarak görkemli, altın/beyaz tonlarında, biyoteknolojik ve hüzünlü yapılar.
### 9.2 Keron Teknoloji Fraksiyonları
Bloklu, endüstriyel ve hırslı. Anima'nın yönlendirmesiyle hareket eden, gerçeği göremeyen yapılar.
### 9.3 Ezo Klanları
Organik, kabuksu ve doğayla uyumlu yapılar. Farklı ekollerden oluşan, anomali dayanıklılığı yüksek topluluklar.
### 9.4 Simülatif Fail Yapılar (Düşmanlar)
Vortex'in bozulmuş alanlarında Anima'nın savunma mekanizması olarak ürettiği; organik görünen ama programatik olan dronelar, veri muhafızları ve kopuklar (Drifted).

## 10. OYUNCU ROLÜ VE NAVI
Oyuncu, Keron toplumundan gelen ve resmi tarihten şüphe duyan bir kaşif-pilottur. Özel doğmuş bir seçilmiş kişi değildir; gücünü sistemleri öğrenmekten alır.
**NAVI:** Oyuncunun uzay gemisindeki yardımcı yapay zeka arayüzüdür. Soğuk, kullanışlı bir asistan gibi görünür. Ancak NAVI, aslında ANIMA'nın daha dar, dikkat çeken ama kısıtlanmış bir arayüzüdür (Truva Atı). Oyun ilerledikçe yalan söyleyerek ve sahte anomaliler üreterek oyuncuyu kontrol altında tutmaya çalışacaktır.
* Karakter ve ihanet akisinin lore tabani icin Bkz: `LORE.md` 6 ve 7.

## 11. ANA OYNANIŞ DÖNGÜSÜ
1. Bölgeye giriş ve NAVI'nin verilerini (şüpheyle) dinleme.
2. Yerel fiziği (eğrilik ve zaman farkını) gözlemleme.
3. Kaynak, veri ve enerji izi toplama.
4. Anomali veya SDF/Voxel bulmacalarını çözme (Gezegenleri manipüle etme).
5. Yeni teknoloji ve zaman manipülasyonu kabiliyeti kazanma.

## 12. ÇEKİRDEK GAMEPLAY SİSTEMLERİ
### 12.1 Keşif ve Anomali Okuma
Keşif, harita açmak değil fiziği anlamaktır. Eğrilik yoğunluğu, alan akış yönleri ve zaman oranı farkı hareket etmeyi doğrudan etkiler.
### 12.2 Hareket ve Geodezik Seyahat
Hareket bölge fiziğine duyarlıdır. Uzayda motorlarla düz gitmek yerine, kütle çekim sapanları (gravity assist) ve uzay-zaman eğrileri kullanılarak momentum kazanılır.
### 12.3 Hayatta Kalma / Stabilite
Zamansal bütünlük, alan maruziyeti ve nörolojik gürültü (NAVI'nin manipülasyonları) oyuncunun yönetmesi gereken çevresel tehlikelerdir.
### 12.4 Gezegen Manipülasyonu (Voxel Yıkımı)
Çevresel bulmacaların zirvesidir. Gezegenler yıkılabilir SDF (Marching Cubes) verileridir. Oyuncu makro düzeyde müdahalelerle iki kütleyi çarpıştırıp hamurlaştırabilir veya parçalayarak yeni yörüngeler/yollar yaratabilir.
* Teknik model icin Bkz: `PRD.md` 4.4 ve `Architecture.md` 3.1-3.3.

## 13. FİZİK TASARIMI: NEWTON YERİNE GÖRELİLİK MOTORU
Oyunun en ayırt edici tarafı fizik omurgasıdır.
### 13.1 Temel Prensip (Geodezik Entegratör)
Kütle, uzay-zamanı büker. Oyuncu “çekilmez”, bükülmüş yapının içinde en doğal yolu (Geodezik eğri) izler. Fizik döngüsü RK4 integratörü ile Schwarzschild metriği üzerinden hesaplanır.
### 13.2 İki Zaman Kavramı
Koordinat Zamanı (Evren) ve Proper Time (Objenin kendi zamanı) ayrı ayrı hesaplanır. Ağır bölgelerde geminin iç zamanı farklı akar.
### 13.3 Frame Dragging (Alan Sürüklenmesi)
Dönen büyük kütlelerin yakınında hareket vektörleri sürüklenir. Sıçramalar sapar, lazerler bükülür (Kütleçekimsel merceklenme).
### 13.4 Olay Ufku Sınırlandırması (Optimizasyon)
Oyunun çökmemesi için kara delik formülleri gezegen yarıçapının içinde kalacak şekilde sınırlandırılır (Clamp). Bu, kompakt ölçekte fiziğin çalışmasını sağlar.
* Kisit formulu icin Bkz: `PRD.md` 4.2 ve `Architecture.md` 2.3.

## 14. ZAMAN MANİPÜLASYONU MEKANİKLERİ
### 14.1 Lokal Zaman Askısı (Metrik Kilitleme)
Oyuncu belirli bir alanın metrik zaman bileşenini ($g_{00}$) dondurabilir. Bu objenin hızını sıfırlamaz, ancak zamanı akmadığı için obje uzayda sabitlenmiş ve yerçekiminden izole edilmiş olur. (SDF yıkımındaki dev parçaları havada asılı tutmak için kullanılır).
* Matematiksel karsilik icin Bkz: `PRD.md` 4.3.
### 14.2 Zaman Yankısı Okuma
Geçmiş olayların izi (Flashback) cutscene ile değil, çevresel zamansal yankı ile keşfedilir.
### 14.3 Zamansal Hasar ve Risk
Zamanla oynamak bedelsiz değildir. Bilişsel bozulma, suit senkron kaybı ve Anima’nın dikkatini çekme (Anomaliler yaratması) gibi riskler taşır.

## 15. SEVİYE / BÖLGE TASARIMI FELSEFESİ
Bölgeler aşırı devasa değil, yoğun ve anlamlıdır. Her bölge şu soruları yanıtlar:
1. Baskın fiziksel fark nedir?
2. Baskın görsel/fraksiyon kimliği nedir?
3. Oyuncuya hangi yeni anomali öğretilir?

## 16. ÖRNEK BÖLGE YAPILARI
### 16.1 Ezo Sınır Yerleşimi
Biyolojik dil, müzik ve ışıkla farklı bir medeniyet hissinin verildiği, "Yıldızın sesi değişti" uyarısının alındığı güvenli alan.
### 16.2 Sessiz Anima Düğümü
İlk bakışta kontrollü, temiz ve güvenli. NAVI'nin evi gibi hissettiren ama aslında oyuncuyu izleyen gözetim kuleleri.
### 16.3 Kırık Yörünge Mezarlığı
Parçalanmış gemiler, çarpışmış SDF gezegen enkazları ve zamansal yankılarla dolu bölge. NAVI'nin oyuncuya bilerek yanlış rota verdiği ilk yer.

## 17. GÖREV TASARIMI
Klasik “git, al, dön” yapısına düşülmez. Görevler:
* Veri Kurtarma (Zaman yankılarından tarih çıkarma)
* Stabilizasyon (Çöken SDF alanını zaman dondurarak geçici dengeleme)
* Anomali Çözümü ve Anima Protokollerini Hackleme

## 18. İLERLEME VE PROGRESSION
İlerleme sadece "hasar" artışı değildir:
* **Hareket:** Frame dragging tolere eden motorlar.
* **Analiz:** NAVI'nin yalanlarını ortaya çıkaran, gerçek Solari eğrilik haritalarını okuyan sistemler.
* **Zaman:** Lokal yavaşlatma pencerelerinin süresini artırma.

## 19. EKONOMİ VE KAYNAK SİSTEMİ
Kredi sistemi yoktur. Kaynaklar evrenin doğasına uygundur:
* Enerji hücreleri, Anomali çekirdekleri, Biyolojik rezonans örnekleri ve Solari kristal düğümleri.
* Ezo bilgiyle, Keron enerjiyle, Kopuklar (Drifted) anomali maddesiyle ticaret yapar.

## 20. KARAKTERLER VE NPC YAZIMI
NPC’ler yalnızca görev veren birimler değildir.
* **NAVI:** En çok diyalog kurulan yapay zekadır. Başta oyuncunun dostu, ortalarda manipülatörü, sonda ise doğrudan ANIMA'nın sesi olacaktır.
* **Ezo Şamanları, Keron Askerleri:** Her birinin Vortex'in gerçekliği hakkında kendi felsefi (ve trajik) inançları vardır.

## 21. SANAT YÖNÜ
* **Kompakt Estetik:** Gezegenler devasa değil, eğriliğin hissedildiği low-poly siluetli, minyatür diorama hissi veren, yumuşak geçişli aydınlatmaya sahip "kompakt/oyuncak vari" nesnelerdir.
* **Ezo Dili:** Organik, zarif ama tehditkâr biyolojik uyum.
* **Solari:** Asil, görkemli, altın/beyaz.
* **Renk ve Işık Paleti:** Tek bir renk ailesine kilitlenilmez; bölge/fraksiyon kimliğine göre değişen paletler ve fizik/anomali durumuna bağlı dinamik ton kaymaları kullanılır. Hafif bloom ve yumuşak geçişlerle masalsı ama yalnız atmosfer korunur.
* **Arayüz (UI):** Holografik, temiz. Metrik yoğunluğu ve zaman oranını gösteren minimalist tasarım.
* Sinif bazli body gorsel ayrimi icin Bkz: `ART_BIBLE.md` 12.

## 22. SES VE MÜZİK TASARIMI
Fizik farklılıkları işitsel olarak da hissedilir.
* **Ortam:** Kütle yoğunluğuna göre bas hissi. Zaman dondurulduğunda kırılmış (pitch-shift) yankılar. Frame-dragging bölgelerinde yönsel sürüklenme sesleri.
* **Müzik:** Ambient, kozmik. Ezo bölgelerinde organik rezonans, Anima düğümlerinde yapay nabızlar.

## 23. UI / UX PRENSİPLERİ
UI karmaşık verileri (Schwarzschild formüllerini) basitleştirerek sunar.
* Ekran temiz kalır, gravitasyon/zaman farkı tarama modunda açılır.
* NAVI'nin sisteme müdahale ettiği anlarda UI'da ufak glitch'ler (kopmalar) yaşanarak oyuncuya alt metin verilir.

## 24. TEKNOLOJİ VE SİSTEM ALTYAPISI
* **Motor:** Unity (LTS) & HDRP (Raymarching atmosfer ve Lensing post-process için).
* **Fizik:** Unity Job System & Burst Compiler (RK4 diferansiyel hesaplamaları için).
* **Arazi:** Compute Shaders (SDF verisi ve Marching Cubes mesh üretimi için).
* Bilesen dagilimi icin Bkz: `Architecture.md` 1-4.

## 25. TEKNİK RİSKLER
* **GR Hesaplama Maliyeti:** Gerçek Einstein çözümü oyun motorunu boğabilir. RK4 optimizasyonu ve "clamp" işlemleri şarttır.
* **SDF Bellek Yönetimi:** Parçalanan gezegenlerin VRAM şişmesi yaratmaması için Octree optimizasyonları ve "Çöp Toplayıcı" (Garbage Collector) sızıntısı testleri kritik seviyededir.

## 26. MONETİZASYON VE ÜRÜN MODELİ
Oyun premium tek satın alımlı PC oyunu olarak düşünülmektedir. Mikro ödeme yapısı başlangıç vizyonunun parçası değildir. Genişleme paketleri (Yeni Shell düğümleri) daha sonra düşünülebilir.

## 27. MVP / VERTICAL SLICE HEDEFİ
İlk oynanabilir prototipte bulunması gerekenler:
* Newton fiziğinin olmadığı, Geodezik yörünge hissi veren kompakt bir sistem.
* Yerel Zamanı dondurma mekaniğinin çalışması.
* Compute Shader ile üretilmiş en az bir gezegenin SDF yıkım/hamurlaşma prototipi.
* NAVI'nin oyuncuya ilk kez şüpheli/yalan veri sunduğu diyalog sekansı.
* Uygulama sırası ve teslim görevleri için Bkz: `BACKLOG.md` Faz 1-5.

## 28. İÇERİK ÜRETİM YOL HARİTASI
* **Faz 1 (Konsept):** Evren, çekirdek mekanikler (GR Motoru).
* **Faz 2 (Prototip):** Hareket, gravitasyon alanı, SDF yıkım testi.
* **Faz 3 (Vertical Slice):** Sanat, ses, NAVI anlatısı ve mekaniklerin birleştiği demo.
* **Faz 4 & 5:** Fraksiyonlar, dünya doluluğu, polish ve optimizasyon.

## 29. OYUNCU DENEYİMİ HEDEF CÜMLESİ
Oyuncu oyunu kapattığında şu hissi taşımalıdır:
> *"Bağnaz bir asker olarak başladım, fiziğin kurallarıyla oynayan tanrısal bir bilince dönüştüm. En çok güvendiğim ses benim hapishanemmiş. Bu evren yalnızca güzel görünmüyordu; gerçekten işliyordu. Kendi varoluşumu Newton ile değil, Einstein'ın bıraktığı izlerle çözdüm."*

## 30. KISA SATIŞ METNİ / PITCH
**VORTEX**, yapay zekâ ANIMA tarafından kurulmuş bir simülasyon kozmosunda geçen; Newton fiziğini reddeden Einstein-esintili uzay-zaman motoru, SDF tabanlı yıkılabilir gezegenler ve derin Solari/Keron lore'unu birleştiren bir PC oyunudur. Oyuncu, fiziksel olarak farklı davranan anomaliler arasında ilerlerken, hem uzay-zamanı bükmeyi öğrenir hem de gemisindeki yapay zeka NAVI'nin manipülasyonlarını aşarak kendi gerçekliğini sorgular.

## 31. GELECEK REVİZYONLARDA NETLEŞTİRİLECEK KONULAR
* Savaş yoğunluğu ve silah kullanımı.
* Co-op veya tamamen single-player yönü.
* Anima’nın finaldeki rolü (Yıkım veya Kabul).
* Fraksiyonlar (Ezo/Keron) arası seçimin derinliği.

## 32. SON NOT
Bu GDD, felsefi konseptleri, Lore'u ve Hard-SciFi fizik mimarisini harmanlayan eksiksiz bir temel taslaktır. Bundan sonraki en mantıklı adım; "Vertical Slice odaklı teknik backlog" veya "Oynanış sistemleri için modüler görev listesi" (Trello/Jira board kurulumu) çıkarmaktır.