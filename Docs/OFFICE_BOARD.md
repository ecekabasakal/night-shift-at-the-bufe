# OFFICE BOARD — Night Shift at the Büfe

Orchestrator tarafından tutulur. Durumlar: Yapılacak / Devam ediyor / Bitti / Engellendi.
Son teslim: 1 Aralık 2026 13:37 PST (TSİ 2 Aralık 00:37).

## Ajan durumu

| Ajan | Klasör | Durum | Not |
|---|---|---|---|
| Orchestrator | `Docs/OFFICE_BOARD.md`, entegrasyon | Devam ediyor | Aşama 0 bitti |
| 1. Game Designer | `Docs/GDD.md`, `Assets/Game/Resources/Data/` | Bitti | 13 müşteri, 7 kural, 118 metin; öz-kontrol temiz |
| 2. Tech Lead | `Assets/Game/Scripts/Core/`, `Docs/ARCHITECTURE.md` | Bitti | Core, csc ile stub'lara karşı 0 hata |
| 3. Gameplay Programmer | `Assets/Game/Scripts/Gameplay/` | Bitti | GameSession + NoticedMeter; 6 senaryo Unity dışında geçti |
| 4. UI Programmer | `Assets/Game/Scripts/UI/` | Bitti | 10 dosya, csc 0 hata; yerleşim Unity'de henüz görülmedi |
| 5. Art Programmer | `Assets/Game/Scripts/Art/` | Devam ediyor | Aşama 2 |
| 6. Audio Programmer | `Assets/Game/Scripts/Audio/` | Devam ediyor | Aşama 2 |
| 7. QA Engineer | `Assets/Game/Tests/`, `Docs/QA_REPORT.md` | Devam ediyor | Testler erken başlatıldı (Core+Gameplay hazır) |
| 8. Build & Release | `Assets/Game/Editor/`, `.gitignore`, `README.md`, `Builds/` | Bitti (Aşama 2) | ProjectBuilder csc 0 hata; Unity'de henüz çalışmadı |

## Aşamalar

- [x] Aşama 0 — Ortam kontrolü
- [x] Aşama 1 — Tasarım ve mimari
- [ ] Aşama 2 — Üretim
- [ ] Aşama 3 — Entegrasyon (0 derleme hatası)
- [ ] Aşama 4 — QA (tüm testler geçiyor)
- [ ] Aşama 5 — Build ve teslim

## Ortam (Aşama 0)

- Unity `6000.6.9f1`, WebGL Build Support kurulu. Proje: `~/NightShift`.
- `gh` girişli, `git` hazır. Playwright kurulu değil.
- Unity binary: `/Applications/Unity/Hub/Editor/6000.6.9f1/Unity.app/Contents/MacOS/Unity`

## Kararlar

1. **Unity'yi sadece Orchestrator çalıştırır** (sırayla). Ajanlar yalnızca dosya yazar; test/derleme çıktısını Orchestrator ajanlara geri iletir.
2. **Şema:** `Docs/DATA_SCHEMA.md` Orchestrator tarafından yazıldı ve onaylandı; Designer ve Tech Lead paralel çalışabilsin diye ikisi de buna uyuyor. Değişiklik isteği buraya yazılır.
3. **Doğru eylem veride açıkça durur** (`correctAction`). Kural değerlendirme = oyuncunun eylemi ile karşılaştırma. Basit, test edilebilir.
4. **Tek runtime assembly:** `Assets/Game/Scripts/NightShift.asmdef` (testler referans verebilsin diye). Editor ve Tests kendi asmdef'lerine sahip.
5. **Paketler:** `com.unity.ugui` 2.6.0 ve `com.unity.test-framework` 1.8.0 manifest'e eklendi (boş projede yoktu).
6. **Input:** Yeni projede `activeInputHandler: 0` (eski Input Manager) geliyor; Input System paketi kurulu olmadığı için 2 (Both) yerine 0 bırakıldı — `Input.GetKey` bununla çalışır.
7. `seen` ve `fired`, üçüncü sonun iki çeşididir (ayrı başlık/metin, aynı "kaybettin" sonu).

8. Tasarım onayı: 7. kural (anahtarlar) eklendi; `instantEnd` müşterileri c11 (seen) ve c13 (fired). Sahne görünümü sınıfı `KioskView` (UnityEditor.SceneView ile çakışmasın diye).
9. Tech Lead varsayımları onaylandı: geceyi bitiren yanlış kararda önce geri bildirim, Continue ile son; `instantEnd` hata eklemez; gösterge geri bildirim sırasında da işler; Play again başlık ekranına döner; düdük sesi `accessory == "whistle"` ile tetiklenir; M tuşu sesi kapatır.
10. Git commit'lerini ve GitHub push'unu Orchestrator yapar (paralel ajanlar git'e dokunmaz).

## Açık işler

- [ ] **UI + Designer:** UI'de `strings.json` dışında kalan sabit İngilizce metinler var (`[1] [2] [3]`, `[ENTER]`, `TAB: close`, `M: sound on/off`). Çeviri için `ui.*` anahtarlarına taşınacak; `ui.controls` metnine M ve Enter eklenecek.
- [ ] **Orchestrator:** WebGL build'de Tab tuşunun tarayıcı odağını canvas'tan kaçırıp kaçırmadığını kontrol et (HUD'daki defter butonu yedek).
- Not: 14 Kasım'da beş ajan kullanım limitine takıldı, limit sıfırlanınca kaldıkları yerden devam ettirildi.
