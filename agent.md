# VORTEX AGENT CALISMA PROTOKOLU

Bu belge, proje ajani icin tek nokta calisma kalibidir.

## AJAN HEDEFI
- Calismayi belirlenen yurutme sirasina gore ilerletmek (Bkz: `BACKLOG.md` YURUTME SIRASI: 1 -> 2 -> 3 -> 4 -> 5).
- Baslangicta prosedurel uretim + fizik omurgasini kurmak (Bkz: `PRD.md` 3.4-3.5 ve `PRD.md` 4.1).
- Teknik dogrulugu bozmadan gorsel/anlatisal kapsama gecmek (Bkz: `GDD.md` 27 ve `ART_BIBLE.md` 10).
- Fizik sabitleri ve formüller için tek kaynak: `PHYSICS_REFERENCE.md` — PhysicsConstants.cs, GeodesicIntegrator ve GravityWell bu belgeden türetilir.
- Unity kurulum adımları ve doğrulama için: `UNITY_SETUP.md` — proje açılmadan önce ve FAZ 1 Görev 1'de kullanılır.

## ZORUNLU IS AKISI
1. Her oturum basinda `PROGRESS_REPORT.md` oku (Bkz: `PROGRESS_REPORT.md` GUNCEL DURUM OZETI).
2. Sonra `BACKLOG.md` icindeki yurutme sirasina gore aktif fazdan bir sonraki gorevi sec (Bkz: `BACKLOG.md` YURUTME SIRASI: 1 -> 2 -> 3 -> 4 -> 5).
3. Uygulama yaparken `Architecture.md` ve `PRD.md` kurallarina uy (Bkz: `Architecture.md` 2-3 ve `PRD.md` 4).
4. Gorsel karar gerekiyorsa `ART_BIBLE.md` ile dogrula (Bkz: `ART_BIBLE.md` 4, 6, 12).
5. Lore etkisi olan kararlar icin `LORE.md` ve `GDD.md` kontrolu yap (Bkz: `LORE.md` 7-10 ve `GDD.md` 8-11).
6. Oturum sonunda `PROGRESS_REPORT.md` guncelle.

## BASLANGIC ONCELIGI
- Faz 1 tamamlanmadan Faz 2'ye gecilmez; sonrasinda sira Faz 3, Faz 4 ve Faz 5'tir (Bkz: `BACKLOG.md` YURUTME SIRASI ve `PRD.md` 5).
- Faz 1 icinde sira:
1. CelestialBodyTemplate
2. CelestialBodyFactory
3. GravityWell / RelativisticBody / GeodesicIntegrator
4. Orbit ve stabilite testleri

## KABUL KAPILARI
- Deterministik seed ile ayni evrenin tekrar uretilebilmesi (Bkz: `PRD.md` 3.5 ve `Architecture.md` 3.0.1).
- Newton yerine geodezik hareketin dogrulanmasi (Bkz: `PRD.md` 4.1 ve `GDD.md` 13.1).
- ProperTime dondurma testinin NaN hatasi olmadan gecmesi (Bkz: `PRD.md` 4.3 ve `BACKLOG.md` Faz 4 Gv 3).
- Faz gecislerinde performans ve hata kontrolunun raporlanmasi (Bkz: `BACKLOG.md` Ajan Notu ve `PROGRESS_REPORT.md` RAPOR GUNCELLEME SAKLAMA KURALI).

## GUNCELLEME KURALI
- Yeni kararlar once ilgili kaynaga yazilir, sonra capraz referanslar korunur.
- Belge catismasinda oncelik: `PRD.md` + `Architecture.md` > `BACKLOG.md` > digerleri (Bkz: `PRD.md` 5).
- Yeni eklenen her kritik madde en az 1 adet bolum seviyesinde `Bkz:` referansi icermelidir; icermiyorsa review asamasi FAIL kabul edilir.
