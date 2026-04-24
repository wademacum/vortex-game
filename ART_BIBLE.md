# VORTEX — ART BIBLE v0.1 (Vertical Slice)

Bu belge, `GDD.md`, `PRD.md`, `LORE.md` ve `BACKLOG.md` ile uyumlu görsel üretim standardıdır.
Amaç tek renkli bir uzay estetiği değil; stilize, okunabilir ve sistemik olarak tutarlı bir görsel çeşitlilik üretmektir.

## 1. SANAT VİZYONU
- Görsel dil **stilize** olacak.
- Fizik sistemi (GR eğrilik, zaman farkı, lensing) sadece mekanikte değil, görselde de hissedilecek.
- "Güzel ama tek tip" yerine "kimlikli ve farklı" gezegen tasarımı hedeflenecek.

## 2. RENK FELSEFESİ (ZORUNLU KURAL)
- Tüm gezegenler mavi tonlarında olmak zorunda değildir.
- Palet üretimi iki eksende çalışır:
1. Bölge/fraksiyon/biome kimliği (Ezo, Keron, Solari, Drifted alanları).
2. Fiziksel durum/anomali etkisi (zaman bozulması, eğrilik yoğunluğu, radyasyon, fırtına).
- Sonuç: Aynı fraksiyonda bile farklı alt-bölge renk karakterleri üretilebilir.

## 3. DOYGUNLUK VE KONTRAST
- Varsayılan hedef: **Doğal-Orta doygunluk**.
- Aşırı neon, aşırı bloom veya sürekli yüksek kontrast kullanılmayacak.
- Vurgu renkleri sadece anlatı/mekanik amaçlı kullanılacak (tehlike, anomali, yönlendirme).

## 4. IŞIKLANDIRMA DİLİ
- Ana yaklaşım: **Yumuşak sinematik aydınlatma**.
- GR uyumu için yerel ışık davranışları fiziksel olaylarla ilişkili okunmalı:
1. Lensing olan bölgede arka plan distorsiyonu ve ışık kayması.
2. Zaman manipülasyonu aktifken bölgesel renk/kontrast sapması.
3. Ağır eğrilikte gölge geçişlerinde uzama/sapma hissi.
- Teknik uygulama HDRP ile yönetilecek; performans için post-process yoğunluğu profil bazlı sınırlandırılacak (Bkz: `Architecture.md` 4.1-4.2).

## 5. STİL SINIRI: STILIZE + FIZIKSEL TUTARLILIK
- Geometri ve renk dilinde stilizasyon serbest.
- Fiziksel sonuçlar ve sistem davranışı (hareket, zaman, eğrilik etkisi) keyfi değiştirilmeyecek.
- "Sanat için mekanik bozma" yapılmayacak; gerekirse sanat parametreleri mekaniğe uyarlanacak.

## 6. GEZEGEN TASARIM KURALLARI
- Her gezegen bir "okunabilir kimlik" taşımalı:
1. Siluet kimliği (uzaktan tanınabilir form).
2. Yüzey kimliği (biyom dağılımı, kırık alanlar, anomalik izler).
3. Işık kimliği (atmosferik saçılma, gece-gündüz karakteri).
- Low-poly siluet korunur; shading hibrittir:
1. Keskin topoğrafik kırıklar sert.
2. Geniş yüzey geçişleri yumuşak.
* Mesh ve shader teknik karşılığı için Bkz: `PRD.md` 3.1-3.3.

## 7. EZO UYUMLU GÖRSEL KANON
- Ezo etkisindeki alanlar organik, ritmik, yaşayan bir yüzey dili taşır.
- Palet örnekleri (kısıt değil):
1. Toprak-kehribar + yosun yeşili.
2. Soluk lavanta + mineral gri.
3. Kiremit-kızıl + kemik beyazı.
4. Gece biyolüminesan vurgular (düşük yoğunlukta).
- Ezo bölgelerinde teknoloji görünümü baskın değil, doğa formu baskın olmalı.
* Ezo anlatı bağlamı için Bkz: `LORE.md` 8 ve `GDD.md` 16.1.

## 8. FRAKSIYON BAZLI KISA REHBER
- Solari: Asil, rafine, altın/beyaz temel; kırık ve hüzünlü yüzey izleri.
- Keron: Endüstriyel, modüler, sert materyal dili.
- Ezo: Organik, yumuşak, ritmik, yaşayan katman hissi.
- Drifted/Anima etkisi: Programatik bozulma, hatalı geometri ve kontrollü yapay renk sapmaları.

## 9. YASAKLAR / KACINILACAK KLİŞELER
- Sürekli aynı gökyüzü tonu veya tek renk baskınlığı.
- Her gezegende aynı su-kara oranı ve aynı biyom dağılımı.
- Aşırı glow ile form okunurluğunu bozma.
- Sadece "güzel" görünen ama gameplay okunurluğunu düşüren kompozisyon.

## 10. VERTICAL SLICE TESLIM KALITE KAPILARI
- En az 3 farklı gezegen kimliği demonstrasyonu (renk, ışık, biyom davranışı farklı).
- En az 1 Ezo uyumlu bölge, 1 Keron etkili bölge, 1 anomalik bölge.
- Fiziksel olaylar (zaman/lensing/eğrilik) görselde ayırt edilebilir olmalı.
- Performans hedefi bozulmadan (hedef cihaz: RTX 3050 / i5-12400f) görsel çeşitlilik korunmalı.
* Uygulama takvimi icin Bkz: `BACKLOG.md` Faz 5 ve urun kabul hedefi icin Bkz: `GDD.md` 27.

## 11. KULLANIM PROTOKOLÜ
- Yeni görsel kararlar önce bu belgeye eklenir.
- Sonra `GDD.md` (sanat yönü), `PRD.md` (teknik etkiler), `BACKLOG.md` (görev kırılımı) güncellenir.
- Çelişki durumunda öncelik: Mekanik doğruluk > oynanış okunurluğu > görsel cila.

## 12. CELESTIAL BODY KALIP SISTEMI (GÖRSEL STANDART)
Tum cisimler tek bir kalip mantigindan turetilir; sinif bazli farklar bu tabloda tanimlanir.

### 12.1. Sinif Matrisi
- Planet: Low-poly SDF yuzey, biyom katmanlari, inilebilir.
- Moon: Planet'e gore daha az atmosfer, daha sert topoğrafya, inilebilir.
- Star: Emissive cekirdek + corona, inilemez, yuksek isik katkisi.
- NeutronStar: Kucuk yaricap, cok yuksek emissive kontrast, asiri lensing etkisi.
- BlackHole: Yuzey yerine olay ufku silueti + accretion disk + guclu ekran bozulmasi.
- Supergiant: Cok genis hacimsel parlama, dusuk detayli ama etkili katmanli gaz hissi.
- AsteroidCluster: Coklu parca kompozisyonu, ortak materyal ailesi, dusuk maliyetli LOD.
* Runtime bileşen karşılığı için Bkz: `Architecture.md` 3.0-3.4.

### 12.2. Kaliptan Gelen Zorunlu Gorsel Parametreler
Her body instance su parametreleri template'ten alir:
1. Ana palet (3-5 renk slotu).
2. Ikincil vurgu paleti (anomali/fraksiyon etkisi).
3. Emissive araligi.
4. Yuzey patern yogunlugu.
5. Atmosfer/haze yogunlugu.
6. LOD uzaklik katsayisi.
* Teknik parametre semasi icin Bkz: `PRD.md` 3.4.

### 12.3. Random Uretimde Tutarlilik Kurallari
- Random secim tamamen serbest degil; sinifin okunurlugunu bozmayacak sinirlar kullanilir.
- Ayni seed ayni cismi vermeli; farkli seed farkli varyant uretmelidir.
- Fraksiyon ve bolge etkisi, template secim agirligini degistirebilir.
- Varyasyon cizgisi: once siluet, sonra renk, sonra mikro detay.
* Deterministik ureti̇m aksisi icin Bkz: `PRD.md` 3.5 ve `Architecture.md` 3.0.1.

### 12.4. Sebastian-Lague Tarzi Uretim Prensibi
- Elle tek tek cisim modellemek yerine parametre uzayi tasarlanir.
- Tasarimci kalibi iyilestirdikce sistem tum yeni cisimlere kalite aktarir.
- Hedef: az manuel is, yuksek cesitlilik, tekrar uretilebilir sonuclar.
* Is onceligi ve uygulama kapilari icin Bkz: `BACKLOG.md` Faz 1 ve `agent.md` BASLANGIC ONCELIGI.
