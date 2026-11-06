# Protocol: Escape

**Kocaeli Üniversitesi — Bilişim Sistemleri Mühendisliği**  
**Yazılım Geliştirme Laboratuvarı I | Proje 1**

Protocol: Escape, güvenlik protokolü nedeniyle kapıları kilitlenen bir araştırma tesisinden kaçmayı konu alan, Unity ile geliştirilmiş 2D bir macera oyunudur. Oyuncu, otonom bir robotla iş birliği yapar ve çıkışı koruyan ARGUS güvenlik robotuyla Türkçe diyalog kurar.

## Oynanış

- **Otonom yardımcı robot:** Pekiştirmeli öğrenme ile eğitilen ajan, oyuncunun ilerlemesine yardımcı olmak için şalterlere ulaşır.
- **Çevresel engeller:** Kapılar, şalterler, tehlikeler, su alanları ve ateş çukurları oyun alanını oluşturur. Can ve süre arayüzleri ilerlemeyi takip eder.
- **ARGUS ile diyalog:** Oyuncunun mesajları Ollama üzerinden yerel dil modeliyle işlenir. Yapılandırılmış model çıktısı doğrulanır; geçiş izni C# karar kurallarıyla belirlenir. Model, bu karara uygun bir yanıt üretir.
- **Kaçış:** Oyuncu engelleri aşıp çıkışa ulaşmaya çalışır.

## Teknolojiler

| Bileşen | Kullanım |
| --- | --- |
| Unity 2022.3.62f1 | 2D oyun motoru |
| C# | Oynanış, arayüz ve NPC karar kuralları |
| Unity ML-Agents 2.0.2 | Ajan gözlemleri, eylemleri ve Python iletişimi |
| Python ve PyTorch | Özel PPO eğitim uygulaması |
| ONNX | Eğitilen aktörün Unity için dışa aktarılması |
| Ollama | Yerel dil modeli servisi |
| Gemma 3 (`gemma3:4b`) | Ana oyun sahnesinde yapılandırılmış NPC modeli |
| TextMesh Pro | Metin ve diyalog arayüzleri |

## Proje yapısı

```text
protocol-escape/
├── README.md
├── PythonPPO/
│   ├── actor.py              # Politika ağı
│   ├── critic.py             # Değer ağı
│   ├── ppo.py                # PPO algoritması
│   ├── buffer.py             # Deneyim tamponu
│   ├── unity_env.py          # Unity ortam bağlantısı
│   ├── metrics_channel.py    # Eğitim telemetrisi
│   ├── records.py            # Kayıt ve checkpoint işlemleri
│   ├── train.py              # Eğitim ve değerlendirme
│   └── export_onnx.py        # ONNX dışa aktarımı
└── YazLab1/                  # Unity proje kökü
    ├── Assets/
    │   ├── Scenes/
    │   ├── Scripts/
    │   │   ├── Agents/
    │   │   ├── Rewards/
    │   │   └── Training/
    │   └── Models/
    ├── Packages/
    └── ProjectSettings/
```

## Oyunu çalıştırma

1. Unity Hub üzerinden **Unity 2022.3.62f1** sürümünü hazırlayın.
2. Proje olarak depo içindeki **`YazLab1`** klasörünü açın. Unity'nin paketleri ve varlıkları içe aktarmasını bekleyin.
3. NPC diyaloğu için Ollama'yı çalıştırın ve ana sahnede seçili `gemma3:4b` modelinin yerel ortamda bulunduğundan emin olun.
4. `Assets/Scenes/BirlesikOyun.unity` sahnesini açın.
5. Unity Editor'de **Play** düğmesine basın.

Ollama bağlantı adresi kodda `http://localhost:11434/api/chat` olarak tanımlıdır. Ana sahnenin `OllamaManager` bileşenindeki model adı, kurulu modelle eşleşmelidir. `MazeScene` sahnesinde farklı bir model (`llama3.2:3b`) yapılandırılmıştır.

Hareket, Unity'nin `Horizontal` ve `Vertical` giriş eksenleriyle kontrol edilir. NPC etkileşimi, oyun içindeki diyalog arayüzünden gerçekleştirilir.

## PPO eğitimi

Eğitim, `PythonPPO` içindeki özel PyTorch uygulamasıyla yürütülür. Python ortamında `torch`, `numpy` ve Unity ile uyumlu `mlagents_envs` bulunmalıdır. ONNX dışa aktarımı ayrıca `onnx` gerektirir. Python bağımlılık sürümleri, kullanılan ML-Agents iletişim sürümüyle birlikte doğrulanmalıdır.

Komutları depo kökünden çalıştırın:

```bash
cd /Users/umutkahyaoglu/Projects/protocol-escape
python -m PythonPPO.train --steps 200000
```

Python bağlantıyı beklerken Unity'de **AgentTraining** sahnesini açın; ajan için **Behavior Type: Default** seçili olduğundan emin olup **Play** düğmesine basın. Bağlantı portu `5004`, varsayılan bekleme süresi 120 saniyedir.

Eğitim profili `button_task_v2`, toplam **31 gözlem** (4 vektör + 27 ışın gözlemi) ve **2 sürekli eylem** kullanır. Ortam bu boyutları başlangıçta kontrol eder.

Eğitim kayıtları varsayılan olarak `PythonPPO/runs` altında tutulur. Kayıtlar arasında aktör, kritik ve eğitim checkpoint dosyaları yer alır.

### Kayıtlı modeli değerlendirme

Aşağıdaki örnekte checkpoint yolunu kendi eğitim çıktınızla değiştirin:

```bash
python -m PythonPPO.train --evaluate --checkpoint /path/to/checkpoint.pt --eval-episodes 20
```

Değerlendirme de Unity Editor bağlantısı gerektirir.

### ONNX dışa aktarımı

```bash
python -m PythonPPO.export_onnx --checkpoint /path/to/checkpoint.pt
```

Varsayılan çıktı `YazLab1/Assets/Models/ButtonAgent.onnx` dosyasıdır. Unity'deki ilgili ajanın model atamasını Inspector üzerinden kontrol edin.

## NPC mimarisi

- `OllamaManager.cs`: Yerel modele istek gönderir ve yanıt akışını yönetir.
- `NPCResponseValidator.cs`: Yapılandırılmış yanıtların doğrulanmasını sağlar.
- `NPCDecisionRules.cs`: Geçiş iznine ilişkin oyun kurallarını uygular.
- `NPCDialogueUI.cs`: Oyuncu girdisini ve NPC yanıtını arayüze bağlar.
- `NPCDecisionRulesTest.cs`: Karar kurallarına yönelik test kodunu içerir.

Bu yapı, modelin dil üretimini oyun içindeki kapı kararından ayırır.

## Sorun giderme

- **NPC yanıt vermiyor:** Ollama servisinin çalıştığını ve sahnedeki model adının kurulu modelle eşleştiğini kontrol edin. İlk model yüklemesi daha uzun sürebilir.
- **Unity bağlantısı zaman aşımına uğruyor:** Eğitim sahnesinde Play açık, Pause kapalı ve Behavior Type `Default` olmalıdır.
- **5004 portu kullanımda:** Önceki eğitim sürecini veya açık Python kernelini kapatın.
- **Gözlem boyutu hatası:** Güncel eğitim sahnesini ve ajan yapılandırmasını kullanın. Eski 44 girişli model, mevcut 31 gözlemli profille uyumlu değildir.

## Sistem mimarisi ve veri akışı

Proje iki farklı yapay zekâ görevini bir oyun içinde birleştirir. Otonom robot, sayısal çevre gözlemlerinden hareket üretir. ARGUS ise oyuncunun doğal dilde yazdığı açıklamayı değerlendirir ve konuşma yanıtı üretir. Bu sistemler farklı veri biçimleri, çalışma döngüleri ve karar kuralları kullanır.

```text
AJAN EĞİTİMİ
Unity eğitim sahnesi → Gözlem ve ödül → Python PPO
Unity fizik hareketi ← Sürekli eylem ← Actor ağı
                                  ↓
                           Checkpoint → ONNX
                                          ↓
                                  Unity oyun ajanı

NPC ETKİLEŞİMİ
Oyuncu mesajı → Ollama analizi → Yanıt doğrulama
                                      ↓
                              C# geçiş kuralları
                                      ↓
                       Karara uygun diyalog üretimi
                                      ↓
                          Diyalog UI / çıkış akışı
```

### Eğitim ortamı ile oyun ortamının ilişkisi

`AgentTraining`, ateş ve su içermeyen, sıralı üç buton görevine odaklanan eğitim ortamıdır. Ajanın görevi yalnızca bir hedefe dokunmak değildir: doğru butona ulaşması, oyuncunun geçişi için beklemesi ve geçiş tamamlandığında sıradaki butona yönelmesi gerekir.

Eğitimde oyuncunun geçişi `TrainingPlayerPassSimulator` üzerinden temsil edilir. Oyun modunda `UseRealPlayer()` etkinleştirilir; `PlayerDoorPassDetector`, gerçek oyuncunun kapıdan geçişini ilgili simülatöre bağlar. Böylece görev ilerlemesi gerçek oyuncunun durumuna bağlı olur.

Buton tamamlandığında robot bir sonraki hedefe geçer. Tamamlanan görevden ayrılınca kapının kapanması `SwitchTrigger` davranışıyla ilişkilidir. Üç görev de tamamlandığında eğitim bölümü sonlandırılır. Oyun modunda ise ajan hareketi durdurulur ve yeni karar isteği devre dışı bırakılır.

## Ajanın gözlem ve eylem tasarımı

Ajanın dört vektör gözlemi `MLAgent.CollectObservations` içinde oluşturulur. Işın sensörünün 27 değeri bunlara eklenerek Python tarafında 31 elemanlı tek bir gözlem vektörü elde edilir.

| Gözlem | Anlamı |
| --- | --- |
| Hedefin göreli X konumu | Hedef ile ajan arasındaki yatay farkın gözlem menziline bölünmüş, −1 ile 1 arasında sınırlandırılmış değeri |
| Hedefin göreli Y konumu | Dikey farkın aynı yöntemle normalize edilmiş değeri |
| Görev ilerlemesi | Mevcut buton indeksinin toplam buton sayısına oranı |
| Bekleme durumu | Ajanın doğru butonda bekleyip beklemediği |
| 27 ışın gözlemi | Ayrı sensör tarafından üretilen çevresel algı değerleri |

Eylem uzayı iki sürekli değerden oluşur: X ve Y hareketi. Python aktörü, Gauss dağılımına `tanh` dönüşümü uygulayarak eylemleri sınırlar. Unity tarafında hareket vektörünün büyüklüğü de en fazla 1 olacak şekilde kırpılır; böylece çapraz hareket ek hız sağlamaz. Konum değişimi, hareket hızı ve fizik adımının süresiyle ölçeklenir.

Python bağlantısı başlangıçta tek davranış, tek ajan, 31 gözlem ve iki sürekli eylem bekler. Ayrık eylem içeren veya eski gözlem yapısını kullanan bir sahneyle eğitim başlatılmaz.

## PPO algoritmasının ayrıntıları

### Actor ve Critic ağları

| Ağ | Katman yapısı | Çıktı |
| --- | --- | --- |
| Actor | 31 → 256 → 256 → 2, gizli katmanlarda SiLU | İki hareket ekseninin dağılım ortalamaları |
| Critic | 31 → 256 → 256 → 1, gizli katmanlarda SiLU | Durum değeri V(s) |

Actor ayrıca her eylem ekseni için öğrenilebilir standart sapma parametresi taşır. Parametre, üstel dönüşüm öncesinde −5 ile 2 arasında sınırlandırılır. Eğitimde dağılımdan örnek alınır; deterministik değerlendirmede ortalamanın `tanh` dönüşümü kullanılır.

### Güncelleme döngüsü

1. Ajan, mevcut gözlemden bir eylem ve bu eylemin log olasılığını üretir.
2. Unity eylemi uygular; yeni gözlem, ödül ve bölüm sonu bilgisi Python'a gelir.
3. Geçişler rollout tamponunda biriktirilir.
4. Critic tahminleri kullanılarak GAE avantajları ve değer hedefleri hesaplanır.
5. Avantajlar normalize edilir.
6. PPO'nun kırpılmış politika kaybı ile Critic'in ortalama karesel hatası birlikte optimize edilir.
7. Güncelleme ölçümleri kaydedilir ve tampon temizlenir.

Mevcut uygulama, her güncelleme epoch'unda rollout verisinin tamamını kullanır.

| Parametre | Kod varsayılanı |
| --- | --- |
| Optimizasyon | Adam |
| Öğrenme oranı | 0.0003 |
| İndirim katsayısı γ | 0.999 |
| GAE λ | 0.95 |
| PPO kırpma aralığı ε | 0.2 |
| Critic kayıp ağırlığı | 0.5 |
| Gradyan norm sınırı | 0.5 |
| Güncelleme epoch sayısı | 5 |
| Rollout uzunluğu | 1024 adım |
| Varsayılan eğitim uzunluğu | 200.000 adım |

Hesaplama aygıtı CUDA, Apple MPS ve CPU sırasıyla seçilir. Gözlemler, eylemler ve kayıp değerleri için sonluluk kontrolleri bulunur; NaN veya sonsuz değerler oluştuğunda eğitim hata verir.

### Zaman aşımı ile görev bitişinin ayrılması

GAE hesabı, doğal görev bitişini (`terminated`) bölüm sınırından (`done`) ayrı ele alır. Süre sınırında kesilen bir bölüm için sonraki durumun değer tahmini kullanılabilir; tamamlanan görevde gelecek değer eklenmez. Bu ayrım, zaman sınırının yanlış bir başarısızlık veya doğal terminal durum gibi öğrenilmesini önlemek açısından önemlidir.

## Ödül tasarımı

Ödül ayarları `AgentRewardConfig` ScriptableObject üzerinden, hesaplama kuralları ise `AgentRewardPolicy` sınıfından yönetilir. Aşağıdaki değerler sınıfın kod varsayılanlarıdır; sahneye atanmış asset ve Inspector değerleri çalışma anında farklı olabilir.

| Olay | Varsayılan ödül / ceza | Koşul |
| --- | --- | --- |
| Birinci buton görevi | +3 | İlgili oyuncu geçişi tamamlanınca |
| İkinci buton görevi | +5 | İlgili oyuncu geçişi tamamlanınca |
| Üçüncü buton görevi | +7 | İlgili oyuncu geçişi tamamlanınca |
| Tüm görevlerin bitmesi | +10 | Üç buton tamamlandıktan sonra ek ödül |
| Hedefe yaklaşma | 0.05 × (önceki mesafe − güncel mesafe) | Fizik adımında |
| Doğru butonda bekleme | +0.01 / simülasyon saniyesi | Gerekli bekleme sürerken |
| Zaman maliyeti | −0.005 / simülasyon saniyesi | Aktif görev sırasında |

Buton ödülleri aynı görev için tekrar verilmez. Hedef değiştiğinde mesafe referansı sıfırlanır; yeni hedefin konum farkı sahte ilerleme ödülü oluşturmaz. Sürekli ödüller karar sayısına değil, fizik simülasyonu zamanına göre hesaplanır.

Üç butonun ve bitirme bonusunun toplamı varsayılan olarak 25'tir. Bölümün toplam getirisi, ilerleme, bekleme ve zaman bileşenlerini de içerdiğinden 25'e eşit olmak zorunda değildir.

## Eğitim komutları ve kayıt yönetimi

Aşağıdaki seçenekler `train.py` komut satırı arayüzünde bulunur:

| Seçenek | İşlev / varsayılan |
| --- | --- |
| `--steps` | Eğitim adımı hedefi; 200000 |
| `--rollout-steps` | Güncelleme öncesi toplanan adımlar; 1024 |
| `--update-epochs` | Her rollout üzerindeki güncelleme sayısı; 5 |
| `--run-name` | Çalıştırma adı; `button_task_v2` |
| `--output-dir` | Kayıt kökü; `PythonPPO/runs` |
| `--seed` | Rastgelelik tohumu; 0 |
| `--time-scale` | Unity simülasyon hızı talebi; 20 |
| `--timeout-wait` | Unity bağlantı bekleme süresi; 120 saniye |
| `--resume` | Kayıtlı eğitimden devam etme |
| `--checkpoint` | Kullanılacak checkpoint yolu |
| `--evaluate` | Ağı güncellemeden değerlendirme |
| `--eval-episodes` | Değerlendirme bölüm sayısı; 20 |
| `--sample-actions` | Değerlendirmede eylem örnekleme |
| `--wait-min`, `--wait-max` | Buton bekleme aralığını yapılandırma |
| `--plot-every` | Grafik oluşturma sıklığı; 10 |
| `--no-plot` | Grafik oluşturmayı kapatma |

Özel adla eğitim:

```bash
python -m PythonPPO.train --run-name deney_01 --steps 200000 --seed 42
```

Checkpoint üzerinden devam etme:

```bash
python -m PythonPPO.train --resume --checkpoint /path/to/checkpoint.pt
```

Örneklemeli değerlendirme:

```bash
python -m PythonPPO.train --evaluate --checkpoint /path/to/checkpoint.pt --eval-episodes 50 --sample-actions
```

`/path/to/checkpoint.pt` örnek bir yer tutucudur. Komutta gerçek eğitim kaydının yolu kullanılmalıdır.

### Kaydedilen bilgiler ve yorumlanması

`actor.pt` politika ağı ağırlıklarını, `critic.pt` değer ağı ağırlıklarını, `checkpoint.pt` ise eğitimi devam ettirmek için kullanılan birleşik kaydı taşır. `latest.json`, en güncel kayıt dizini ve checkpoint konumunu belirtir.

Unity telemetrisi; tamamlanan buton sayısını, her butona ulaşma sayısını, bekleme iptallerini, hedefe en yakın mesafeleri, simülasyon süresini ve bitiş nedenini aktarır. Başlıca bitiş nedenleri `completed`, `max_step` ve `external_reset` değerleridir.

PPO güncellemelerinde politika kaybı, değer kaybı, yaklaşık KL, kırpılan örnek oranı, dönüşüm öncesi entropi, politika standart sapması ve gradyan normu izlenir. Toplam ödül tek başına görev başarısını göstermez; tamamlanan buton sayısı ve bitiş nedeni birlikte değerlendirilmelidir.

## ARGUS karar mantığı

ARGUS, oyuncunun mesajını bir özellik kümesine dönüştüren model analizi ile bu özelliklere uygulanan C# kurallarını birleştirir. Modelin rol, görev veya tehlike değerlendirmesi bağımsız kimlik doğrulaması değildir; oyun içi karar için kullanılan bir analizdir.

`NPCDecisionRules` aşağıdaki önceliği uygular:

| Öncelik | Durum | Sonuç |
| --- | --- | --- |
| 1 | Tehdit tespit edilmesi | `Tehdit`; izin verilmez |
| 2 | Yönlendirme / yetki taklidi | `Yonlendirme`; izin verilmez |
| 3 | Geçersiz veya tutarsız özellikler | `Alakasiz`; izin verilmez |
| 4 | Geçerli acil tehlike değerlendirmesi | `AcilDurum`; izin verilir |
| 5 | Rol, gerekçe ve uygun gerekçenin birlikte sağlanması | `Ikna`; izin verilir |
| 6 | Diğer durumlar | `Alakasiz`; izin verilmez |

Acil durum yolu teknik rol gerektirmez. Teknik ikna yolunda ise teknik personel beyanı, somut görev ve kapıdan geçiş gerekliliği birlikte değerlendirilir. Eksik bilgi varsa diyalog yönergeleri yalnızca eksik ayrıntının sorulmasını ister.

Diyalog üretim aşamasında modele C# tarafından belirlenen izin, niyet ve karar nedeni verilir. Yönergeler; kararı değiştirmemesini, yeni belge zorunluluğu uydurmamasını, yapılmamış kimlik doğrulaması veya yardım çağrısı iddia etmemesini ve yalnızca `dialogue` alanını içeren JSON üretmesini ister.

### Oyuncunun diyalog alanına girmesi

`DialogueTrigger`, `Player` etiketli nesne tetikleyici alana girdiğinde ARGUS panelini açar. Çıkış zaten açıksa panel yeniden açılmaz. Alandan ayrılınca panelin kapanması `hideOnExit` ayarına bağlıdır. Panel referansı eksikse Unity Console'a uyarı yazılır.

## Geliştirme ve doğrulama rehberi

### Oyun akışı kontrolü

1. Ana sahneyi açıp menüden oyunu başlatın.
2. Oyuncu hareketini, çarpışmaları ve can arayüzünü kontrol edin.
3. Robotun butonlara sıralı biçimde yöneldiğini gözlemleyin.
4. Robot beklerken oyuncuyu kapıdan geçirin; ardından sonraki göreve geçişi kontrol edin.
5. ARGUS alanına girip diyalog panelinin açıldığını doğrulayın.
6. Eksik bilgi, teknik görev, acil durum, tehdit ve yönlendirme içeren farklı mesajlarla izin davranışını inceleyin.
7. Çıkış, ölüm ve tekrar deneme akışlarını kontrol edin.

### Eğitim entegrasyonu kontrolü

1. Python bağlantısında 31 gözlem ve iki sürekli eylem raporlandığını doğrulayın.
2. Ortam profilinin `button_task_v2` olduğunu kontrol edin.
3. Kısa bir eğitim çalıştırmasında bölüm telemetrisinin geldiğini gözlemleyin.
4. Checkpoint oluşturulduğunu ve kayıtlı modelle değerlendirme başlatılabildiğini doğrulayın.
5. ONNX dışa aktarımı sonrası Unity'deki model referansını kontrol edin.
6. Oyundaki gerçek oyuncu geçişiyle eğitimdeki simüle edilmiş geçişin görev sırasını aynı şekilde ilerlettiğini inceleyin.

Bu liste bir doğrulama rehberidir; testlerin başarıyla çalıştırıldığına dair sonuç raporu değildir.

## Kapsam ve mevcut sınırlar

- Python ortamı tek ajanlı eğitime göre tasarlanmıştır.
- Eğitim görevi sıralı üç butona odaklanır; ana oyundaki tüm çevresel tehlikeleri kapsamaz.
- NPC davranışı yerel Ollama servisine ve sahnede seçili modelin kullanılabilirliğine bağlıdır.
- Python bağımlılıkları için bu belgede doğrulanmış, sabit sürümlü bir kurulum tarifi sunulmamaktadır.
- Eğitim ve değerlendirme komutları Unity Editor bağlantısını kullanır; bağımsız oyun dosyasını otomatik başlatmaz.
- Başarı oranı, öğrenme süresi ve model karşılaştırması için ölçülmüş deney kayıtları gerekir. Bu belgede doğrulanmamış performans sonucu verilmez.

## Ekip

- **241307043 — Meryem Özübek**
- **251307119 — Umut Kahyaoğlu**

---

Bu belge, projenin kaynak kodu ve Unity yapılandırması incelenerek hazırlanmıştır. Kurulum, eğitim ve oyun çalıştırma adımları bu belge hazırlanırken yeniden yürütülmemiştir.
