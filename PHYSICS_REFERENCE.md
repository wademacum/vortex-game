# Dosya: physics_reference.md
# VORTEX EVRENİ — FİZİK SABİTLERİ VE FORMÜL REFERANSI

> **Amaç:** `GeodesicIntegrator`, `GravityWell` ve `RelativisticBody` implementasyonlarında doğrudan kullanılacak analitik formülleri, oyun içi sabit değerlerini ve birim ölçeğini tek kaynakta sabitlemek. Bu belge olmadan `PhysicsConstants.cs` yazılamaz.
> (Bkz: `Architecture.md` 2.1-2.3 ve `PRD.md` 4.1-4.2)

---

## 1. BİRİM ÖLÇEĞİ KARARI

| Kavram | Değer | Gerekçe |
|---|---|---|
| 1 Unity birimi | 1 km (yaklaşık) | Kompakt gezegen estetiği — PRD.md 1: yarıçap 50-150 Unity birimi = 50-150 km |
| Tipik gezegen yarıçapı | 100 Unity birimi | Referans kütleye karşılık gelen taban değer |
| Güvenli yörünge mesafesi | 200 Unity birimi (2R) | Yüzeyin 1R üstü, oyuncu en az bu mesafede başlar |
| Hedef yörünge periyodu | ~60 saniye | Oyuncunun beklemeye gerek duymadığı minimum süre |

**Buradan türetilen yörünge hızı:**
$$v_{orbit} = \frac{2\pi r_{orbit}}{T} = \frac{2\pi \times 200}{60} \approx 21 \text{ birim/s}$$

---

## 2. OYUN İÇİ FİZİK SABİTLERİ (`PhysicsConstants.cs`)

```csharp
public static class PhysicsConstants
{
    /// Oyun içi ışık hızı (birim/s). Gerçek c'nin ölçeklenmiş hali.
    /// v/c ≈ 0.21 orbital hızda → gözlemlenebilir relativistik etkiler.
    public const float C = 100f;

    /// Evrensel çekim sabiti (oyun birimi). G=1 seçildi; kütle M birimleri buna göre tanımlı.
    public const float G = 1f;

    /// C² sabiti (hesaplama kolaylığı için önceden hesaplanmış)
    public const float C_SQUARED = C * C; // 10_000f

    /// Birimsiz kütle birimi türetme:
    /// Stabil yörünge için GM = v² × r = 21² × 200 ≈ 88_200
    /// Tipik gezegen kütlesi M ≈ 88_200 (G=1 olduğu için GM = M)
    public const float TYPICAL_PLANET_MASS = 88_200f;

    /// Oynanabilir hızlanma bandı: 0.6c - 0.8c
    public const float GAMEPLAY_SPEED_MIN_RATIO = 0.60f;
    public const float GAMEPLAY_SPEED_MAX_RATIO = 0.80f;

    /// Soft limit: bu oranın üstünde thrust verimi düşürülür.
    public const float SOFT_SPEED_LIMIT_RATIO = 0.85f;

    /// Hard limit: hız bu oranı geçemez (numerik stabilite koruması).
    public const float HARD_SPEED_LIMIT_RATIO = 0.95f;
}
```

### 2.1. IŞIK HIZI LİMİT POLİTİKASI

$$v_{soft} = 0.85c, \quad v_{hard} = 0.95c$$

* `v <= v_soft`: Normal thrust eğrisi.
* `v_soft < v < v_hard`: Kademeli thrust düşümü (soft clamp).
* `v >= v_hard`: Sert clamp ile hız `v_hard` değerine çekilir.

> **Not:** Bu politika oynanışta relativistik hissi korurken RK4 integratöründe sayısal patlamayı (NaN/Infinity) azaltır.

### 2.2. YEREL ZAMAN (LOCAL DELTA TIME) KURALI

Her `RelativisticBody` için nihai zaman adımı obje bazında hesaplanır:

$$\Delta t_{local} = \Delta t_{global} \times \alpha_{SR}(v) \times \alpha_{GR}(r, M)$$

* $\alpha_{SR}(v)$: Lorentz temelli hız çarpanı.
* $\alpha_{GR}(r, M)$: Kütle/mesafe temelli (Schwarzschild) çarpan.

**Uygulama kuralı:** Çekirdek fizik güncellemesi global `Time.timeScale` ile değil, yalnızca `\Delta t_{local}` ile yapılır.

---

## 3. SCHWARZSCHILD YARIYAPI GÜVENLİK SINIRI

$$r_s = \frac{2GM}{c^2}$$

Oyun içi sabitlerle:

$$r_s = \frac{2 \times 1 \times M}{10000} = \frac{M}{5000}$$

**`GravityWell.OnValidate()` içinde zorunlu clamp kuralı** (`PRD.md` 4.2):

$$r_s < R_{gezegen} - 10 \quad \Rightarrow \quad M < (R_{gezegen} - 10) \times 5000$$

| Gezegen Yarıçapı (R) | Maksimum Güvenli Kütle (M) | r_s (max) |
|---|---|---|
| 50 birim | 200,000 | 40 birim |
| 100 birim | 450,000 | 90 birim |
| 150 birim | 700,000 | 140 birim |

> **Kara Delik İstisnası:** `BlackHoleTemplate` için r_s > R kural ihlali değildir — kara delik tanımı gereğidir. Bu cisimler için clamp DEVRE DIŞI bırakılır, ama oyuncu bu cismin içine giremez (ör. InvisibleWall veya trigger).

---

## 4. SCHWARZSCHILD METRİĞİ

Koordinatlar: $(t, r, \theta, \phi)$ → indisler $(0, 1, 2, 3)$

$$ds^2 = -\left(1 - \frac{r_s}{r}\right)c^2\,dt^2 + \left(1 - \frac{r_s}{r}\right)^{-1}dr^2 + r^2\,d\theta^2 + r^2\sin^2\theta\,d\phi^2$$

Kısaltma: $f(r) = 1 - \dfrac{r_s}{r}$

Metrik bileşenleri:
$$g_{tt} = -f(r)\,c^2, \quad g_{rr} = \frac{1}{f(r)}, \quad g_{\theta\theta} = r^2, \quad g_{\phi\phi} = r^2\sin^2\theta$$

---

## 5. SIFIRDAN OLMAYAN CHRISTOFFEL SEMBOLLERİ

Tüm simetrik olmayan permütasyonlar dahil ($\Gamma^\mu_{\alpha\beta} = \Gamma^\mu_{\beta\alpha}$):

### $\Gamma^t$ (zaman bileşeni)

$$\Gamma^t_{tr} = \Gamma^t_{rt} = \frac{r_s}{2r(r - r_s)}$$

### $\Gamma^r$ (radyal bileşen)

$$\Gamma^r_{tt} = \frac{c^2\, r_s\,(r - r_s)}{2r^3}$$

$$\Gamma^r_{rr} = -\frac{r_s}{2r(r - r_s)}$$

$$\Gamma^r_{\theta\theta} = -(r - r_s)$$

$$\Gamma^r_{\phi\phi} = -(r - r_s)\sin^2\theta$$

### $\Gamma^\theta$ (polar bileşen)

$$\Gamma^\theta_{r\theta} = \Gamma^\theta_{\theta r} = \frac{1}{r}$$

$$\Gamma^\theta_{\phi\phi} = -\sin\theta\cos\theta$$

### $\Gamma^\phi$ (azimutal bileşen)

$$\Gamma^\phi_{r\phi} = \Gamma^\phi_{\phi r} = \frac{1}{r}$$

$$\Gamma^\phi_{\theta\phi} = \Gamma^\phi_{\phi\theta} = \frac{\cos\theta}{\sin\theta}$$

---

## 6. GEODEZİK DENKLEMLER (RK4 İÇİN AÇIK FORMLAR)

Durum vektörü (8 boyutlu): $\mathbf{s} = (t,\; r,\; \theta,\; \phi,\; u^t,\; u^r,\; u^\theta,\; u^\phi)$

Burada $u^\mu = dx^\mu/d\tau$.

### Konumların türevleri (trivial):

$$\frac{dt}{d\tau} = u^t, \quad \frac{dr}{d\tau} = u^r, \quad \frac{d\theta}{d\tau} = u^\theta, \quad \frac{d\phi}{d\tau} = u^\phi$$

### Hızların türevleri:

$$\frac{du^t}{d\tau} = -2\,\Gamma^t_{tr}\; u^t u^r = -\frac{r_s}{r(r-r_s)}\; u^t u^r$$

$$\frac{du^r}{d\tau} = -\Gamma^r_{tt}(u^t)^2 - \Gamma^r_{rr}(u^r)^2 - \Gamma^r_{\theta\theta}(u^\theta)^2 - \Gamma^r_{\phi\phi}(u^\phi)^2$$

$$= -\frac{c^2 r_s(r-r_s)}{2r^3}(u^t)^2 + \frac{r_s}{2r(r-r_s)}(u^r)^2 + (r-r_s)(u^\theta)^2 + (r-r_s)\sin^2\theta\,(u^\phi)^2$$

$$\frac{du^\theta}{d\tau} = -\frac{2}{r}\; u^r u^\theta + \sin\theta\cos\theta\;(u^\phi)^2$$

$$\frac{du^\phi}{d\tau} = -\frac{2}{r}\; u^r u^\phi - \frac{2\cos\theta}{\sin\theta}\; u^\theta u^\phi$$

---

## 7. RK4 UYGULAMA ŞEMASI (`GeodesicIntegrator` için)

```
// Adım boyutu: dtau = ProperTime × Time.fixedDeltaTime
// (ProperTime == 0 ise tüm hesabı atla — freeze guard)

k1 = f(s)
k2 = f(s + dtau/2 × k1)
k3 = f(s + dtau/2 × k2)
k4 = f(s + dtau   × k3)

s_new = s + (dtau/6) × (k1 + 2k2 + 2k3 + k4)
```

Burada `f(s)` → 8 bileşenli türev vektörü (Bölüm 6'daki denklemler).

**NaN koruma:** Her adım sonrası `math.isnan(u^r)` || `math.isnan(u^t)` kontrolü. Tetiklenirse adımı atla, konsola `[GeodesicIntegrator] NaN detected at r={r}` logla.

---

## 8. KÜRESEL ↔ KARTEZYEN DÖNÜŞÜM

Unity Transform güncellemesi için:

$$x = r\sin\theta\cos\phi, \quad y = r\cos\theta, \quad z = r\sin\theta\sin\phi$$

> **Unity eksen notu:** Unity Y-up koordinat sistemi kullanır. Yukardaki dönüşümde $\theta=0$ → kutup (Y ekseni pozitif).

Tersi (Transform'dan küresel'e dönüş):

$$r = \sqrt{x^2 + y^2 + z^2}$$
$$\theta = \arccos\!\left(\frac{y}{r}\right)$$
$$\phi = \text{atan2}(z,\; x)$$

---

## 9. BAŞLANGIÇ KOŞULLARI (Test Sahnesi İçin)

Dairesel yörünge için başlangıç 4-hızı ($\theta = \pi/2$ ekvatorial düzlem):

$$u^\phi_0 = \sqrt{\frac{GM}{r^3 - r\,r_s\,r^2/2}} \approx \sqrt{\frac{GM}{r^3}}\quad (r \gg r_s)$$

$$u^t_0 = \frac{1}{\sqrt{f(r) - r^2(u^\phi_0)^2/c^2}}$$

$$u^r_0 = 0, \quad u^\theta_0 = 0$$

Tipik test değerleri (M = 88,200, c = 100, G = 1):
- r₀ = 200, θ₀ = π/2, φ₀ = 0
- u^φ₀ ≈ 0.105 rad/s → doğrusal hız ≈ 21 birim/s ✓
- r_s = 88200/5000 = 17.6 birim → r >> r_s ✓ (küresel simetrik limit)

---

## 10. ÇOKLU GRAVİTY WELL SÜPERPOZISYONU

Birden fazla `GravityWell` için her cismin potansiyeline katkısı toplandığında Schwarzschild metriği geçersiz hale gelir (artık tam Genel Görelilik çözümü gerekir — post-Newtonian yaklaşım dışında yok). 

**Oyun kararı (PRD.md 4.1 ile uyumlu):**
- Integratör, her frame içinde aktif `GravityWell`'leri ağırlığa göre sıralayarak en baskın olan için Schwarzschild metriği kullanır.
- Hiyerarşi: en yakın `GravityWell` baskın alınır (diğerleri pertürbasyon olarak eklenmez — Faz 1-4'te bu yeterlidir).
- Faz 5 sonrasında çoklu-merkez için Majumdar-Papapetrou yaklaşımı değerlendirilebilir (kapsam dışı, şimdilik kural: tek dominant kuyu).

---

*Bu belge `PhysicsConstants.cs`, `GeodesicIntegrator.cs` ve `GravityWell.cs` yazımında doğrudan kaynak olarak kullanılmalıdır. Sabit değerlerde değişiklik yapılırsa bu belgede önce güncelleme yapılır, ardından kod güncellenir. (Bkz: `agent.md` GUNCELLEME KURALI)*
