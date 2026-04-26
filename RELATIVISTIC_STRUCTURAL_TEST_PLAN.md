# RELATIVISTIC STRUCTURAL TEST PLAN

Durum: Draft
Tarih: 2026-04-25
Kapsam: StructuralResponseBody + MeshNodeDeformer + GeodesicSystem tidal/contact entegrasyonu
Backlog Etiket Kumesi: TEST-STR-*

---

## 1) Test Stratejisi

Bu plan, procedural mesh ureten cisimlerde asagidaki fizik davranislarini dogrular:
- Katı yuzey temasinda penetrasyon cozumunun stabilitesi
- Tidal alan altinda node-bazli deformasyon (spagettification)
- Yapisal gerilim birikimi, kirilma tetigi, cokme ilerlemesi
- Yildiz siniflarinda nova tetik akisinin dogrulanmasi
- Performans ve LOD hedefleri

Testler 3 seviyede yurutulur:
1. Component-level manuel/yarı-otomatik test (Inspector + sahne)
2. Senaryo testleri (collision, black-hole approach, stellar collapse)
3. Performans/profiler testleri (hedef FPS ve GC kontrolu)

---

## 2) Test Ortamlari

Zorunlu sahneler:
- Assets/Scenes/Tests/GeodesicOrbitTest.unity
- Assets/Scenes/Tests/CollisionTest.unity (olusturulacak/guncellenecek)
- Assets/Scenes/Tests/StructuralDeformationTest.unity (olusturulacak)

Zorunlu bilesenler:
- GeodesicSystem
- GravityWell (en az 1 buyuk kutle)
- RelativisticBody
- StructuralResponseBody
- MeshNodeDeformer

---

## 3) Etiketli Test Listesi

### TEST-STR-001: Mesh Instance Guvenligi
Amac: Deformer'in shared mesh'i bozmadan instance uzerinde calistigini dogrulamak.
Kurulum:
- Ayni prefabtan 2 cisim spawn et.
- Sadece birinde MeshNodeDeformer aktif olsun.
Beklenti:
- Deforme olan sadece hedef instance olur.
- Diger prefab instance'inin geometri verisi degismez.
Kabul:
- PASS: Global/shared mesh bozulmasi yok.

### TEST-STR-002: Tidal Eksen Dogrulugu
Amac: En guclu gravity well yonunde eksensel uzama dogrulamak.
Kurulum:
- Cisim + iki gravity well (biri baskin kutle).
Beklenti:
- Uzama baskin kuyu ekseninde olur.
Kabul:
- PASS: Gozlemsel eksen uyumu var, ters eksen yok.

### TEST-STR-003: Deform-Recovery Davranisi
Amac: Alan azaldiginda kontrollu geri donus.
Kurulum:
- Cismi kuyuya yaklastir/uzaklastir.
Beklenti:
- Deform artar, sonra recoveryRate ile rest shape'e doner.
Kabul:
- PASS: Ziplama/pop olmadan geri donus.

### TEST-STR-004: Coklu MeshFilter Hiyerarsi
Amac: Child mesh hiyerarsilerinde stabil deform.
Kurulum:
- Parent + birden cok child MeshFilter.
Beklenti:
- Tum hedef meshler uyumlu deforme olur.
Kabul:
- PASS: Eksik/hatali mesh kalmaz.

### TEST-STR-010: Temas Gerilimi Birikimi
Amac: Contact stress'in penetrasyon+closing speed ile artmasi.
Kurulum:
- Gezegen-uydu kontrollu temas senaryosu.
Beklenti:
- compressionStress degeri temaslarda artar.
Kabul:
- PASS: Inspector/runtime log ile artis gorulur.

### TEST-STR-011: Tidal Gerilim Birikimi
Amac: Kuyuya yaklasirken tensionStress artisinin dogrulanmasi.
Kurulum:
- Kara delige yaklaşan cisim.
Beklenti:
- Tidal arttikca tensionStress artar.
Kabul:
- PASS: Monoton artis trendi.

### TEST-STR-012: Fracture Event Tetikleme
Amac: Gerilim esigi asilinca FractureTriggered tetiklenmesi.
Kurulum:
- Dusuk fractureThreshold ile kontrollu test.
Beklenti:
- Event bir kez tetiklenir.
Kabul:
- PASS: Cift tetik/yok tetik yok.

### TEST-STR-013: Birikimli Darbe Davranisi
Amac: Tek darbe yerine coklu darbede kirilma.
Kurulum:
- Art arda kucuk temaslar.
Beklenti:
- Birikimli etkiyle esik asilir.
Kabul:
- PASS: Kademeli birikim gorulur.

### TEST-STR-020: Core Pressure Dengesi
Amac: cekirdek basinci yeterliyse cisim stabil kalir.
Kurulum:
- corePressureSupport yuksek, ayni tidal yuk.
Beklenti:
- collapseProgress dusuk kalir/azalir.
Kabul:
- PASS: Cokme ilerlemesi baskilanir.

### TEST-STR-021: Collapse Progress Artisi
Amac: Basinc yetersizse cokme ilerlemesi.
Kurulum:
- corePressureSupport dusuk, yuksek yuk.
Beklenti:
- collapseProgress zamanla artar.
Kabul:
- PASS: Kritik esige yakinsar.

### TEST-STR-022: Nova Event Tetikleme
Amac: Yildiz sinifinda nova esik asiminda event tetigi.
Kurulum:
- canTriggerNova=true, dusuk novaThreshold.
Beklenti:
- NovaTriggered olayi tetiklenir.
Kabul:
- PASS: Olay akisi tek ve beklenen anda.

### TEST-STR-023: Nova Sonrasi Fizik Tutarliligi
Amac: Nova tetiginden sonra geodesic akisin NaN/Infinity uretmemesi.
Kurulum:
- Nova tetiklenmis sahnede 30 sn sim.
Beklenti:
- Stabil transform ve velocity degerleri.
Kabul:
- PASS: Konsol temiz.

### TEST-STR-030: LOD Deformasyon Performans Eşiği
Amac: Uzak cisimlerde deformasyon maliyetini azaltarak FPS koruma.
Kurulum:
- 50 aktif cisim, farkli mesafelerde.
Beklenti:
- Ortalama FPS >= 60 (hedef donanim).
Kabul:
- PASS: Profiler frametime hedef icinde.

### TEST-STR-031: LOD Gecis Pop Kontrolu
Amac: LOD gecisinde ani geometri siçramasi olmamasi.
Kurulum:
- Kamera ileri-geri sweep.
Beklenti:
- Pop/titreme minimal.
Kabul:
- PASS: Gozlemsel kalite kabul edilir.

### TEST-STR-032: GC ve Bellek Tutarliligi
Amac: Uzun testte mesh realloc/GC patlamasi olmamasi.
Kurulum:
- 10 dk surekli sim.
Beklenti:
- GC spike kabul siniri altinda.
Kabul:
- PASS: Kritikten buyuk spike yok.

### TEST-STR-900: Backlog-Test Izlenebilirlik Kontrolu
Amac: Her RS backlog maddesinin en az bir test etiketi olmasi.
Kurulum:
- BACKLOG.md ve bu dosyayi satir bazli capraz kontrol et.
Beklenti:
- Etiketsiz backlog maddesi kalmaz.
Kabul:
- PASS: Tum maddelerde TEST-STR referansi var.

---

## 4) Raporlama Formati

Her test kosusu sonunda PROGRESS raporuna asagidaki formatla ozet gecilir:
- Etiket: TEST-STR-XXX
- Sonuc: PASS/FAIL
- Sahne: <test scene>
- Not: Kisa bulgu
- Aksiyon: Gerekli backlog gorevi

---

## 5) Oncelikli Uygulama Sirasi

1. TEST-STR-001, 002, 003, 004
2. TEST-STR-010, 011, 012, 013
3. TEST-STR-020, 021, 022, 023
4. TEST-STR-030, 031, 032
5. TEST-STR-900
