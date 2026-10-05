# Protocol: Escape

**Kocaeli Üniversitesi - Bilişim Sistemleri Mühendisliği**  
**Yazılım Geliştirme Laboratuvarı-I | Proje 1**

---

## 📌 Proje Hakkında
**Protocol: Escape**, yapay zekâ araştırmaları yapılan bir tesiste geçen 2D macera oyunudur. Bir sistem hatası nedeniyle güvenlik protokolü aktifleşmiş ve tüm kapılar kilitlenmiştir. 

Oyuncu, kilitli odalardan kaçmak için paralel test bölgesinde çalışan **Otonom Yapay Zekâ Robotu (ML-Agent)** ile iş birliği yapmak ve çıkıştaki **Yerel LLM Destekli Güvenlik NPC'sini (ARIA)** ikna etmek zorundadır.

---

## 🎮 Temel Oyun Mekanikleri & Aşamalar

1. **Otonom AI Companion (ML-Agents):** 
   - Oyuncunun geçeceği kapıları açmak için diğer labirentteki şalterlere/butonlara otonom olarak ulaşır (Reinforcement Learning / PPO).
2. **Sosyal / Mantıksal Engel (Yerel LLM NPC):**
   - Çıkış kapısını koruyan güvenlik yapay zekâsı (ARIA) ile Ollama üzerinden çalışan yerel bir dil modeli aracılığıyla serbest metin diyalog kurulur.
   - LLM'den dönen yapılandırılmış (JSON) çıktı ile kapının kilit durumu kontrol edilir.

---

## 🛠️ Kullanılan Teknolojiler
- **Oyun Motoru:** Unity 2D (C#)
- **Yapay Zekâ (Otonom):** Unity ML-Agents Toolkit (PPO)
- **Yerel LLM:** Ollama (Phi-3 / Llama-3.2 / Gemma-2)
- **Sürüm Kontrolü:** Git & GitHub

---

## 👥 Ekip Üyeleri
- ** 241307043 Meryem Özübek **
- ** 251307119 Umut Kahyaoğlu**

---

> *Proje detaylı teknik raporu ve akış diyagramları geliştirme sürecinde güncellenecektir.*